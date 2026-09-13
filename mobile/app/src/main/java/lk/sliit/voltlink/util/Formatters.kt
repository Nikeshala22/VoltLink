// -----------------------------------------------------------------------------
// File        : Formatters.kt
// Project     : VoltLink Mobile - Smart Solar Microgrid Trading System
// Description : Conversion between the UTC timestamps the Web API works in and
//               the local times shown to the user, plus small display helpers.
//
//               The service stores and returns every time in UTC; this file is
//               the only place in the application where a conversion happens.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

package lk.sliit.voltlink.util

import java.text.ParseException
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

object Formatters {

    // An ISO 8601 timestamp as .NET writes it: whole seconds, then an optional
    // fraction of any length, then an optional zone.
    private val ISO_TIMESTAMP =
        Regex("""^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d+))?(Z|[+-]\d{2}:\d{2})?$""")

    /**
     * Parses a UTC timestamp from the API into a Date, or null when the value
     * is missing or in a shape that is not recognised.
     */
    fun parseUtc(value: String?): Date? {
        if (value.isNullOrBlank()) return null

        val match = ISO_TIMESTAMP.matchEntire(value.trim()) ?: return null
        val (wholeSeconds, fraction, zone) = match.destructured

        // The service writes as many fractional digits as the value holds, up
        // to seven. SimpleDateFormat has no field for a fraction: "S" counts
        // milliseconds, so ".5651093" would be read as 5,651,093 ms and move
        // the time on by over an hour and a half. The fraction is therefore
        // cut or padded to exactly three digits before parsing.
        val millis = fraction.padEnd(3, '0').take(3)

        // A value with no zone marker is still UTC, because the service stores
        // and returns every time in UTC.
        val offset = if (zone.isEmpty() || zone == "Z") "+00:00" else zone

        try {
            val format = SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSXXX", Locale.UK)
            format.isLenient = false

            return format.parse("$wholeSeconds.$millis$offset")
        } catch (ignored: ParseException) {
            // Shaped like a timestamp but not a real one, such as month 13.
        }

        return null
    }

    /** Formats a UTC timestamp as a local date and time, for example 06 Sep 2026, 19:30. */
    fun dateTime(utc: String?): String {
        val date = parseUtc(utc) ?: return "—"
        return SimpleDateFormat("dd MMM yyyy, HH:mm", Locale.UK).format(date)
    }

    /** Formats a UTC timestamp as a local date only. */
    fun date(utc: String?): String {
        val date = parseUtc(utc) ?: return "—"
        return SimpleDateFormat("dd MMM yyyy", Locale.UK).format(date)
    }

    /** Formats a UTC timestamp as a local time only. */
    fun time(utc: String?): String {
        val date = parseUtc(utc) ?: return "—"
        return SimpleDateFormat("HH:mm", Locale.UK).format(date)
    }

    /**
     * Describes a booking window as a date with a start and end time, which is
     * how the booking screens present it.
     */
    fun window(startUtc: String?, endUtc: String?): String {
        val start = parseUtc(startUtc) ?: return "—"
        val end = parseUtc(endUtc)

        val dayFormat = SimpleDateFormat("dd MMM yyyy", Locale.UK)
        val timeFormat = SimpleDateFormat("HH:mm", Locale.UK)

        return if (end == null) {
            "${dayFormat.format(start)}, ${timeFormat.format(start)}"
        } else {
            "${dayFormat.format(start)}, ${timeFormat.format(start)} – ${timeFormat.format(end)}"
        }
    }

    /**
     * Presents a distance in the unit that reads most naturally: metres up to
     * a kilometre, kilometres beyond that.
     */
    fun distance(metres: Double): String {
        return if (metres < 1000) {
            "${metres.toInt()} m"
        } else {
            String.format(Locale.UK, "%.1f km", metres / 1000.0)
        }
    }

    /** Formats an energy volume with its unit. */
    fun energy(kwh: Double): String = String.format(Locale.UK, "%.1f kWh", kwh)
}
