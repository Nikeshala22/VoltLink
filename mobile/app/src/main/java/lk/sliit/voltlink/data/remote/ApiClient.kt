// -----------------------------------------------------------------------------
// File        : ApiClient.kt
// Project     : VoltLink Mobile - Smart Solar Microgrid Trading System
// Description : Builds the Retrofit client used for every call to the Web API,
//               attaches the stored access token to each request, and turns a
//               refused request into an ApiException carrying the service's own
//               error code and message.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

package lk.sliit.voltlink.data.remote

import com.google.gson.Gson
import lk.sliit.voltlink.BuildConfig
import lk.sliit.voltlink.data.local.LocalStore
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Response
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import kotlinx.coroutines.CancellationException
import java.io.IOException
import java.util.concurrent.TimeUnit

/**
 * An error reported by the Web API, or a failure to reach it at all.
 *
 * The code is the stable identifier the service sends, such as
 * RESERVATION_OUTSIDE_7_DAYS, so a screen can react to a particular rule while
 * still showing the service's own wording for everything else.
 */
class ApiException(
    val errorCode: String,
    val statusCode: Int,
    override val message: String
) : Exception(message) {

    /** True when the session is missing or has expired. */
    val isUnauthorised: Boolean get() = statusCode == 401

    /** True when the request never reached the service. */
    val isNetworkFailure: Boolean get() = statusCode == 0

    companion object {
        const val CODE_NETWORK = "NETWORK_ERROR"
        const val CODE_UNKNOWN = "UNKNOWN_ERROR"
    }
}

object ApiClient {

    private val gson = Gson()

    /**
     * Called when the service refuses a request as unauthorised, which means
     * the stored token has expired or been revoked. Set by AppServices, which
     * owns the session and the navigation back to the sign in screen.
     */
    var onSessionRejected: (() -> Unit)? = null

    /**
     * Creates the Retrofit implementation of the API description.
     *
     * The token is read from the local database on every request rather than
     * captured once, so signing in or out takes effect immediately without the
     * client having to be rebuilt.
     */
    fun create(store: LocalStore): VoltLinkApi {
        val authInterceptor = Interceptor { chain ->
            val requestBuilder = chain.request().newBuilder()

            val token = store.getSession()?.accessToken
            if (!token.isNullOrBlank()) {
                requestBuilder.addHeader("Authorization", "Bearer $token")
            }

            chain.proceed(requestBuilder.build())
        }

        val logging = HttpLoggingInterceptor().apply {
            // Bodies are logged only in a debug build, so a release build never
            // writes tokens or personal data into logcat.
            level = if (BuildConfig.DEBUG) {
                HttpLoggingInterceptor.Level.BODY
            } else {
                HttpLoggingInterceptor.Level.NONE
            }

            // Even in a debug build the token itself stays out of logcat, where
            // any tool attached to the device could otherwise copy it and act
            // as the signed in user until it expires.
            redactHeader("Authorization")
        }

        val client = OkHttpClient.Builder()
            .addInterceptor(authInterceptor)
            .addInterceptor(logging)
            // A handset on a weak connection should fail in a few seconds with
            // a clear message rather than appear to hang.
            .connectTimeout(20, TimeUnit.SECONDS)
            .readTimeout(30, TimeUnit.SECONDS)
            .build()

        return Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create(gson))
            .build()
            .create(VoltLinkApi::class.java)
    }

    /**
     * Reports whether the stored token is already past its expiry time. Set by
     * AppServices, which owns the session.
     */
    var isSessionExpired: (() -> Boolean)? = null

    /** Error code for a request stopped on the device because the token had expired. */
    const val CODE_SESSION_EXPIRED = "SESSION_EXPIRED"

    /**
     * Runs an API call and returns its body, or throws an ApiException that
     * explains why it failed.
     *
     * Every screen goes through this, so the token handling and the error
     * translation exist in exactly one place.
     */
    suspend fun <T> call(block: suspend () -> Response<T>): T {
        // When Android restarts a process it killed in the background, it
        // reopens the last screen directly and the splash screen's expiry check
        // never runs. A token already known to have expired is certain to be
        // refused, and sending it anyway kept the user waiting on the round
        // trip, over twenty seconds on a cold IIS start, before sign in opened.
        if (isSessionExpired?.invoke() == true) {
            onSessionRejected?.invoke()
            throw ApiException(CODE_SESSION_EXPIRED, 401, defaultMessageFor(401))
        }

        val response = try {
            block()
        } catch (cancelled: CancellationException) {
            // The screen that started the call has closed. Cancellation has to
            // propagate untouched, or the coroutine would carry on regardless.
            throw cancelled
        } catch (io: IOException) {
            // The request never reached the service: no connection, the wrong
            // address, or cleartext traffic blocked for that host.
            throw ApiException(
                ApiException.CODE_NETWORK,
                0,
                "Could not reach the VoltLink service. Check that the API is running " +
                    "and that this device can reach ${BuildConfig.API_BASE_URL}"
            )
        } catch (unreadable: Exception) {
            // Gson throws when a body is not the JSON it expects, for example an
            // IIS error page returned in place of the API's reply. Every screen
            // only catches ApiException, so anything else here would crash the
            // application instead of showing a message.
            throw ApiException(
                ApiException.CODE_UNKNOWN,
                -1,
                "The VoltLink service sent a response this app could not read."
            )
        }

        if (response.isSuccessful) {
            val body = response.body()

            if (body != null) return body

            throw ApiException(
                ApiException.CODE_UNKNOWN,
                response.code(),
                "The service returned an empty response."
            )
        }

        // An expired token is refused on every screen alike, so it is dealt
        // with here once rather than each screen having to notice it.
        if (response.code() == 401) {
            onSessionRejected?.invoke()
        }

        throw toApiException(response)
    }

    /**
     * Reads the ProblemDetails body of a failed response so the message shown
     * to the user is the one the service wrote.
     */
    private fun <T> toApiException(response: Response<T>): ApiException {
        val status = response.code()

        // A failed response does not always carry a JSON body; an unauthorised
        // reply from the authentication layer has none at all.
        val raw = try {
            response.errorBody()?.string()
        } catch (io: IOException) {
            null
        }

        if (!raw.isNullOrBlank()) {
            try {
                val problem = gson.fromJson(raw, ProblemDetailsDto::class.java)

                if (problem != null) {
                    val code = problem.errorCode ?: problem.title ?: ApiException.CODE_UNKNOWN
                    val message = problem.detail ?: defaultMessageFor(status)

                    return ApiException(code, status, message)
                }
            } catch (parseFailure: Exception) {
                // The body was not ProblemDetails, so fall through to a
                // generic message rather than showing raw text to the user.
            }
        }

        return ApiException(ApiException.CODE_UNKNOWN, status, defaultMessageFor(status))
    }

    /** Wording used when the service gave no explanation of its own. */
    private fun defaultMessageFor(status: Int): String = when (status) {
        401 -> "Your session has expired. Please sign in again."
        403 -> "You do not have permission to do that."
        404 -> "The requested item could not be found."
        else -> "The request failed with status $status."
    }
}
