// -----------------------------------------------------------------------------
// File        : SplashActivity.kt
// Project     : VoltLink Mobile - Smart Solar Microgrid Trading System
// Description : Entry point of the application. Reads the session held in the
//               local SQLite database and sends the user straight to the home
//               screen for their role, or to sign in when there is no session.
// Author      : IT23215924    M U D Gunatilake
// -----------------------------------------------------------------------------

package lk.sliit.voltlink.ui

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import lk.sliit.voltlink.AppServices
import lk.sliit.voltlink.data.remote.ApiConstants
import lk.sliit.voltlink.ui.operator.OperatorHomeActivity
import lk.sliit.voltlink.ui.prosumer.ProsumerHomeActivity

class SplashActivity : AppCompatActivity() {

    /**
     * Decides where to send the user, then finishes so this screen never
     * appears in the back stack.
     */
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)

        // A token that has already expired would only be refused by the
        // service, after the home screen had opened and waited on the network,
        // so it is discarded here and the user is asked to sign in straight
        // away instead.
        if (!AppServices.hasValidSession()) {
            AppServices.signOut()
        }

        // The session is read from SQLite, which is why a user who has signed
        // in once is taken straight to their home screen on later launches.
        val session = AppServices.store.getSession()

        val destination = when {
            session == null -> LoginActivity::class.java

            // Grid operators and prosumers use the same application but are
            // given different home screens, as the specification requires.
            session.role == ApiConstants.ROLE_PROSUMER -> ProsumerHomeActivity::class.java

            else -> OperatorHomeActivity::class.java
        }

        startActivity(Intent(this, destination))
        finish()
    }
}
