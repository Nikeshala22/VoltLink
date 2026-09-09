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
import lk.sliit.voltlink.data.local.LocalStore
import lk.sliit.voltlink.data.remote.ApiClient
import lk.sliit.voltlink.data.remote.VoltLinkApi

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

    private var localStore: LocalStore? = null
    private var voltLinkApi: VoltLinkApi? = null

    /**
     * Creates the services. Called once from the Application class.
     */
    fun initialise(context: Context) {
        val store = LocalStore(context.applicationContext)

        localStore = store
        voltLinkApi = ApiClient.create(store)
    }

    /** Local SQLite access: the session and the cached data. */
    val store: LocalStore
        get() = localStore ?: error("AppServices.initialise has not been called.")

    /** The Web API client. */
    val api: VoltLinkApi
        get() = voltLinkApi ?: error("AppServices.initialise has not been called.")

    /** True when somebody is signed in on this device. */
    fun isSignedIn(): Boolean = store.getSession() != null

    /** Signs out, discarding the session and any cached personal data. */
    fun signOut() {
        store.clearSession()
    }
}
