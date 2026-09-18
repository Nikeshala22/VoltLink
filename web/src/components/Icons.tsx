// -----------------------------------------------------------------------------
// File        : components/Icons.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : The line icon set used across the console.
//
//               The icons are drawn inline as SVG rather than pulled from an
//               icon font or a package. Nothing extra has to be downloaded
//               before the first paint, every glyph inherits the surrounding
//               text colour through "currentColor", and the set stays small
//               because only the symbols this application actually uses are
//               defined here.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import type { SVGProps } from 'react'

/** Shared props. Callers size an icon with a Tailwind class, e.g. "h-4 w-4". */
type IconProps = SVGProps<SVGSVGElement>

/**
 * Common attributes for every glyph in the set.
 *
 * The icons are decorative wherever they sit beside a text label, so they are
 * hidden from assistive technology by default; a caller that uses an icon on
 * its own passes its own aria-label and overrides this.
 */
function base(props: IconProps) {
  return {
    viewBox: '0 0 24 24',
    fill: 'none',
    stroke: 'currentColor',
    strokeWidth: 1.75,
    strokeLinecap: 'round' as const,
    strokeLinejoin: 'round' as const,
    'aria-hidden': true,
    ...props,
  }
}

/* -------------------------------------------------------------------------- */
/* Brand                                                                      */
/* -------------------------------------------------------------------------- */

/**
 * The VoltLink mark: a bolt formed inside a linked hexagonal cell, standing
 * for energy moving between connected nodes of the microgrid.
 *
 * It is filled rather than stroked so it stays legible at the small sizes used
 * in the navigation and the browser tab.
 */
export function LogoMark({ className = 'h-5 w-5' }: { className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="none" aria-hidden="true">
      <path
        d="M12 1.6 21 6.6v10.8L12 22.4 3 17.4V6.6z"
        stroke="currentColor"
        strokeWidth="1.6"
        strokeLinejoin="round"
        opacity="0.45"
      />
      <path d="M13.1 5.9 7.6 13.2h3.6l-.7 4.9 5.5-7.3h-3.6z" fill="currentColor" />
    </svg>
  )
}

/* -------------------------------------------------------------------------- */
/* Navigation                                                                 */
/* -------------------------------------------------------------------------- */

/** Dashboard: a panel split into tiles. */
export function IconDashboard(props: IconProps) {
  return (
    <svg {...base(props)}>
      <rect x="3" y="3" width="7.5" height="7.5" rx="2" />
      <rect x="13.5" y="3" width="7.5" height="4.5" rx="2" />
      <rect x="13.5" y="10.5" width="7.5" height="10.5" rx="2" />
      <rect x="3" y="13.5" width="7.5" height="7.5" rx="2" />
    </svg>
  )
}

/** Microgrid node: a hub with three connected spurs. */
export function IconNode(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="12" cy="12" r="3" />
      <circle cx="12" cy="3.6" r="1.8" />
      <circle cx="19.3" cy="16.2" r="1.8" />
      <circle cx="4.7" cy="16.2" r="1.8" />
      <path d="M12 5.4V9M14.6 13.6l3.1 1.7M9.4 13.6l-3.1 1.7" />
    </svg>
  )
}

/** Reservation: energy moving in both directions between two parties. */
export function IconExchange(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M4 8h13l-3.2-3.2M20 16H7l3.2 3.2" />
    </svg>
  )
}

/** Prosumer: the sun, for a household that generates its own power. */
export function IconSolar(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="12" cy="12" r="4" />
      <path d="M12 2v2.2M12 19.8V22M2 12h2.2M19.8 12H22M4.9 4.9l1.6 1.6M17.5 17.5l1.6 1.6M19.1 4.9l-1.6 1.6M6.5 17.5l-1.6 1.6" />
    </svg>
  )
}

/** Pending activation: an inbox awaiting a decision. */
export function IconInbox(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M3 13.5 5.4 5.2A2 2 0 0 1 7.3 3.8h9.4a2 2 0 0 1 1.9 1.4L21 13.5" />
      <path d="M3 13.5h4.5l1.2 2.4h6.6l1.2-2.4H21v4.7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z" />
    </svg>
  )
}

/** System users: a person beside a cog. */
export function IconUsers(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="9" cy="8" r="3.4" />
      <path d="M2.8 20a6.2 6.2 0 0 1 12.4 0" />
      <circle cx="18.2" cy="15.6" r="2.1" />
      <path d="M18.2 11.6v1.2M18.2 18.4v1.2M21.6 13.6l-1 .6M15.8 17l-1 .6M21.6 17.6l-1-.6M15.8 14.2l-1-.6" />
    </svg>
  )
}

/* -------------------------------------------------------------------------- */
/* Actions and states                                                         */
/* -------------------------------------------------------------------------- */

/** Menu: opens the navigation drawer on a small screen. */
export function IconMenu(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M3.5 6.5h17M3.5 12h17M3.5 17.5h17" />
    </svg>
  )
}

/** Close. */
export function IconClose(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M6 6l12 12M18 6L6 18" />
    </svg>
  )
}

/** Search. */
export function IconSearch(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="10.5" cy="10.5" r="6.5" />
      <path d="M15.4 15.4 20.5 20.5" />
    </svg>
  )
}

/** Refresh. */
export function IconRefresh(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M20.4 12a8.4 8.4 0 1 1-2.5-6" />
      <path d="M20.6 3.4v5h-5" />
    </svg>
  )
}

/** Plus, for the create actions. */
export function IconPlus(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M12 5v14M5 12h14" />
    </svg>
  )
}

/** Sign out. */
export function IconLogout(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M14.5 3.5h3a2 2 0 0 1 2 2v13a2 2 0 0 1-2 2h-3" />
      <path d="M10 16.5 14.5 12 10 7.5M14.5 12H4" />
    </svg>
  )
}

/** Chevron pointing left, used by the back links. */
export function IconChevronLeft(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M14.5 5.5 8 12l6.5 6.5" />
    </svg>
  )
}

/** Chevron pointing right, used by the breadcrumb separators. */
export function IconChevronRight(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M9.5 5.5 16 12l-6.5 6.5" />
    </svg>
  )
}

/** Sun, for the light theme. */
export function IconSun(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="12" cy="12" r="4.2" />
      <path d="M12 2.4v2M12 19.6v2M2.4 12h2M19.6 12h2M5.1 5.1l1.4 1.4M17.5 17.5l1.4 1.4M18.9 5.1l-1.4 1.4M6.5 17.5l-1.4 1.4" />
    </svg>
  )
}

/** Moon, for the dark theme. */
export function IconMoon(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M20 14.2A8.4 8.4 0 0 1 9.8 4a8.4 8.4 0 1 0 10.2 10.2z" />
    </svg>
  )
}

/** Monitor, for following the operating system. */
export function IconMonitor(props: IconProps) {
  return (
    <svg {...base(props)}>
      <rect x="2.8" y="4" width="18.4" height="12.5" rx="2" />
      <path d="M8.5 20h7M12 16.5V20" />
    </svg>
  )
}

/** Warning triangle, used by the error banner. */
export function IconAlert(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M10.3 3.7 2.5 17.2A2 2 0 0 0 4.2 20.2h15.6a2 2 0 0 0 1.7-3L13.7 3.7a2 2 0 0 0-3.4 0z" />
      <path d="M12 9v4.2M12 16.6h.01" />
    </svg>
  )
}

/** Tick in a circle, used by the success banner. */
export function IconCheck(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="12" cy="12" r="9" />
      <path d="M8.2 12.3 11 15l4.8-5.4" />
    </svg>
  )
}

/** Letter "i" in a circle, used by the informational banner. */
export function IconInfo(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 11v5M12 7.8h.01" />
    </svg>
  )
}

/** Bolt, used wherever an energy figure is shown. */
export function IconBolt(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M13.4 2.6 5.8 13.3h5l-1.2 8.1 7.6-10.7h-5z" />
    </svg>
  )
}

/** Clock, used by the scheduling figures. */
export function IconClock(props: IconProps) {
  return (
    <svg {...base(props)}>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 6.8V12l3.4 2" />
    </svg>
  )
}

/** Map pin, used by the location details. */
export function IconPin(props: IconProps) {
  return (
    <svg {...base(props)}>
      <path d="M12 21.5s7-5.6 7-11a7 7 0 1 0-14 0c0 5.4 7 11 7 11z" />
      <circle cx="12" cy="10.3" r="2.6" />
    </svg>
  )
}

/** Battery, used by the storage figures. */
export function IconBattery(props: IconProps) {
  return (
    <svg {...base(props)}>
      <rect x="2.5" y="7" width="16" height="10" rx="2.5" />
      <path d="M21.5 10.5v3" />
      <path d="M6 10.5v3M9.5 10.5v3M13 10.5v3" />
    </svg>
  )
}
