// -----------------------------------------------------------------------------
// File        : ApiClientTest.kt
// Project     : VoltLink Mobile - Smart Solar Microgrid Trading System
// Description : Checks that a request made with a token already known to have
//               expired is stopped on the device. When Android restores a
//               screen after killing the process the splash screen's check
//               never runs, and the expired token used to be sent anyway,
//               leaving the user waiting on a refusal before sign in opened.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

package lk.sliit.voltlink.data.remote

import kotlinx.coroutines.runBlocking
import org.junit.After
import org.junit.Assert.assertEquals
import org.junit.Assert.assertFalse
import org.junit.Assert.assertTrue
import org.junit.Assert.fail
import org.junit.Test
import retrofit2.Response

class ApiClientTest {

    /** The hooks are global, so each test leaves them as it found them. */
    @After
    fun resetHooks() {
        ApiClient.isSessionExpired = null
        ApiClient.onSessionRejected = null
    }

    @Test
    fun expiredSession_isRejectedWithoutSendingTheRequest() {
        var requestSent = false
        var rejections = 0

        ApiClient.isSessionExpired = { true }
        ApiClient.onSessionRejected = { rejections++ }

        try {
            runBlocking {
                ApiClient.call {
                    requestSent = true
                    Response.success("unused")
                }
            }
            fail("An expired session should not return a body.")
        } catch (error: ApiException) {
            // Screens close on isUnauthorised, so the code has to report 401.
            assertTrue(error.isUnauthorised)
            assertEquals(ApiClient.CODE_SESSION_EXPIRED, error.errorCode)
        }

        assertFalse(requestSent)

        // Discarding the session and opening sign in happens exactly once.
        assertEquals(1, rejections)
    }

    @Test
    fun validSession_sendsTheRequest() {
        ApiClient.isSessionExpired = { false }

        val body = runBlocking { ApiClient.call { Response.success("ok") } }

        assertEquals("ok", body)
    }

    @Test
    fun noHookSet_sendsTheRequest() {
        // Sign in and registration run before any session exists.
        val body = runBlocking { ApiClient.call { Response.success("ok") } }

        assertEquals("ok", body)
    }
}
