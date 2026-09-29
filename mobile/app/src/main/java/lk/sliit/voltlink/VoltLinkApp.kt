// -----------------------------------------------------------------------------
// File        : VoltLinkApp.kt
// Project     : VoltLink Mobile - Smart Solar Microgrid Trading System
// Description : Application class. Creates the local database access and the
//               API client once, when the process starts, and exposes them to
//               every screen.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

package lk.sliit.voltlink

import android.app.Application
import android.content.Context
import android.content.Intent
import lk.sliit.voltlink.data.local.LocalStore
import lk.sliit.voltlink.data.remote.ApiClient
import lk.sliit.voltlink.data.remote.VoltLinkApi
import lk.sliit.voltlink.ui.LoginActivity
import lk.sliit.voltlink.util.Formatters
import java.util.Date

class VoltLinkApp : Application() {

    /**
     * Builds the shared services before any screen is created.
     */
    override fun onCreate() {
        super.onCreate()
        AppServices.initialise(this)
    }
}

/**
 * The shared services of the application.
 *
 * Held in one place so that the SQLite helper and the Retrofit client are each
 * created once for the whole process. Creating a Retrofit client per screen
 * would build a new connection pool every time a screen opened.
 */
object AppServices {

    private var appContext: Context? = null
    private var localStore: LocalStore? = null
    private var voltLinkApi: VoltLinkApi? = null

    /**
     * Creates the services. Called once from the Application class.
     */
    fun initialise(context: Context) {
        val application = context.applicationContext
        val store = LocalStore(application)

        appContext = application
        localStore = store
        voltLinkApi = ApiClient.create(store)

        ApiClient.onSessionRejected = { handleSessionRejected() }
        ApiClient.isSessionExpired = { hasExpiredSession() }
    }

    /** Local SQLite access: the session and the cached data. */
    val store: LocalStore
        get() = localStore ?: error("AppServices.initialise has not been called.")

    /** The Web API client. */
    val api: VoltLinkApi
        get() = voltLinkApi ?: error("AppServices.initialise has not been called.")

    /**
     * True when somebody is signed in on this device and their token has not
     * yet expired.
     *
     * An expiry time that cannot be read is treated as still valid: the
     * service remains the authority and refuses the token if it is not.
     */
    fun hasValidSession(): Boolean {
        val session = store.getSession() ?: return false
        val expiresAt = Formatters.parseUtc(session.expiresAtUtc) ?: return true

        return expiresAt.after(Date())
    }

    /**
     * True when a session is stored but its token has already expired.
     *
     * Unlike hasValidSession this is false when nobody is signed in, so the
     * sign in and registration requests are never stopped by it.
     */
    fun hasExpiredSession(): Boolean {
        val session = store.getSession() ?: return false
        val expiresAt = Formatters.parseUtc(session.expiresAtUtc) ?: return false

        return !expiresAt.after(Date())
    }

    /** Signs out, discarding the session and any cached personal data. */
    fun signOut() {
        store.clearSession()
    }

    /**
     * Responds to the service refusing the stored token by discarding it and
     * opening the sign in screen on a fresh task.
     *
     * Several requests can be refused at once when a screen loads. Only the
     * first finds a session to discard, so the sign in screen is opened once.
     * API responses are handled on the main thread, so the check and the clear
     * cannot interleave.
     */
    private fun handleSessionRejected() {
        val context = appContext ?: return
        if (store.getSession() == null) return

        signOut()

        val intent = Intent(context, LoginActivity::class.java).addFlags(
            Intent.FLAG_ACTIVITY_NEW_TASK or Intent.FLAG_ACTIVITY_CLEAR_TASK
        )
        context.startActivity(intent)
    }
}
