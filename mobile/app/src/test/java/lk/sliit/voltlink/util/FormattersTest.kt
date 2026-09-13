// -----------------------------------------------------------------------------
// File        : FormattersTest.kt
// Project     : VoltLink Mobile - Smart Solar Microgrid Trading System
// Description : Checks that every timestamp shape the Web API produces is read
//               as the instant it names. .NET writes a variable number of
//               fractional digits, and reading a seven digit fraction as
//               milliseconds once shifted displayed times by over an hour.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

package lk.sliit.voltlink.util

import org.junit.Assert.assertEquals
import org.junit.Assert.assertNull
import org.junit.Test
import java.time.Instant

class FormattersTest {

    /** Epoch milliseconds of the instant the parser returned, or null. */
    private fun parsed(value: String?): Long? = Formatters.parseUtc(value)?.time

    /** Epoch milliseconds of an instant, read by the JDK's own ISO parser. */
    private fun expected(iso: String): Long = Instant.parse(iso).toEpochMilli()

    @Test
    fun wholeSeconds_areParsed() {
        assertEquals(expected("2026-09-13T03:57:02Z"), parsed("2026-09-13T03:57:02Z"))
    }

    @Test
    fun sevenDigitFraction_isNotReadAsMilliseconds() {
        // The shape .NET writes for DateTime.UtcNow.
        assertEquals(
            expected("2026-09-13T03:57:02.565Z"),
            parsed("2026-09-13T03:57:02.5651093Z")
        )
    }

    @Test
    fun threeDigitFraction_isParsed() {
        assertEquals(
            expected("2026-09-13T03:57:02.565Z"),
            parsed("2026-09-13T03:57:02.565Z")
        )
    }

    @Test
    fun shortFraction_isReadAsTenthsAndHundredths() {
        // ".56" is 560 milliseconds, not 56.
        assertEquals(
            expected("2026-09-13T03:57:02.560Z"),
            parsed("2026-09-13T03:57:02.56Z")
        )
    }

    @Test
    fun missingZone_isTreatedAsUtc() {
        assertEquals(expected("2026-09-13T08:30:00Z"), parsed("2026-09-13T08:30:00"))
    }

    @Test
    fun explicitOffset_isApplied() {
        assertEquals(
            expected("2026-09-13T03:00:00Z"),
            parsed("2026-09-13T08:30:00.000+05:30")
        )
    }

    @Test
    fun unrecognisedValues_returnNull() {
        assertNull(parsed(null))
        assertNull(parsed(""))
        assertNull(parsed("not a date"))
        assertNull(parsed("2026-13-40T99:00:00Z"))
    }
}
