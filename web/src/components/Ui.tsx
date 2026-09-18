// -----------------------------------------------------------------------------
// File        : components/Ui.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Small presentational building blocks shared by every screen:
//               status badges, page headers, toolbars, empty and loading
//               states, alerts, statistic tiles and a modal dialog.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import type { ReactNode } from 'react'
import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import type { ReservationStatus, UserRole } from '../types'
import {
  IconAlert,
  IconCheck,
  IconChevronLeft,
  IconChevronRight,
  IconClose,
  IconInfo,
  IconSearch,
} from './Icons'

/* -------------------------------------------------------------------------- */
/* Status badges                                                              */
/* -------------------------------------------------------------------------- */

// Each reservation status gets its own colour so a long list can be scanned at
// a glance rather than read word by word.
const RESERVATION_STATUS_STYLES: Record<ReservationStatus, string> = {
  Pending: 'bg-warn-bg text-warn-fg',
  Approved: 'bg-info-bg text-info-fg',
  Completed: 'bg-success-bg text-success-fg',
  Cancelled: 'bg-neutral-bg text-neutral-fg',
  Rejected: 'bg-danger-bg text-danger-fg',
}

/** Coloured label for a reservation status. */
export function StatusBadge({ status }: { status: ReservationStatus }) {
  return (
    <span className={`badge ${RESERVATION_STATUS_STYLES[status]}`}>
      <span className="badge-dot" />
      {status}
    </span>
  )
}

const ROLE_STYLES: Record<UserRole, string> = {
  Backoffice: 'bg-violet-bg text-violet-fg',
  GridOperator: 'bg-info-bg text-info-fg',
  Prosumer: 'bg-success-bg text-success-fg',
}

/** Coloured label for a user role. */
export function RoleBadge({ role }: { role: UserRole }) {
  // The stored value is one word; a space makes "Grid Operator" read properly.
  const label = role === 'GridOperator' ? 'Grid Operator' : role

  return (
    <span className={`badge ${ROLE_STYLES[role]}`}>
      <span className="badge-dot" />
      {label}
    </span>
  )
}

/** Green or grey label showing whether an account or station is active. */
export function ActiveBadge({ isActive }: { isActive: boolean }) {
  return (
    <span
      className={`badge ${isActive ? 'bg-success-bg text-success-fg' : 'bg-neutral-bg text-neutral-fg'}`}
    >
      <span className="badge-dot" />
      {isActive ? 'Active' : 'Inactive'}
    </span>
  )
}

/* -------------------------------------------------------------------------- */
/* Page furniture                                                             */
/* -------------------------------------------------------------------------- */

/** One step in the trail above a page title. */
export interface Crumb {
  label: string
  to?: string
}

/**
 * Title, optional description and an action area at the top of a page.
 *
 * An optional breadcrumb trail sits above the title so a detail screen says
 * where it came from without each page having to draw its own back link.
 */
export function PageHeader({
  title,
  description,
  actions,
  crumbs,
  eyebrow,
}: {
  title: string
  description?: string
  actions?: ReactNode
  crumbs?: Crumb[]
  eyebrow?: string
}) {
  return (
    <div className="mb-6">
      {crumbs && crumbs.length > 0 && (
        <nav aria-label="Breadcrumb" className="mb-3 flex items-center gap-1 text-xs text-ink-400">
          {crumbs.map((crumb, index) => (
            <span key={`${crumb.label}-${index}`} className="flex items-center gap-1">
              {index > 0 && <IconChevronRight className="h-3.5 w-3.5 opacity-60" />}
              {crumb.to ? (
                <Link
                  to={crumb.to}
                  className="rounded transition-colors hover:text-brand-600 hover:underline"
                >
                  {crumb.label}
                </Link>
              ) : (
                <span className="text-ink-500">{crumb.label}</span>
              )}
            </span>
          ))}
        </nav>
      )}

      <div className="flex flex-wrap items-start justify-between gap-4">
        <div className="min-w-0">
          {eyebrow && (
            <p className="mb-1 text-xs font-semibold uppercase tracking-[0.14em] text-brand-600">
              {eyebrow}
            </p>
          )}
          <h1 className="text-[26px] font-semibold leading-tight tracking-tight text-ink-900">
            {title}
          </h1>
          {description && <p className="mt-1.5 max-w-2xl text-sm text-ink-500">{description}</p>}
        </div>

        {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
      </div>
    </div>
  )
}

/** Link back to a listing page, shown at the top of the detail screens. */
export function BackLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <Link
      to={to}
      className="mb-4 inline-flex items-center gap-1.5 text-sm font-medium text-ink-500 transition-colors hover:text-brand-600"
    >
      <IconChevronLeft className="h-4 w-4" />
      {children}
    </Link>
  )
}

/**
 * A search box with the magnifier drawn inside it.
 *
 * Used by every listing screen, so the filter control looks and behaves the
 * same wherever a list can be narrowed down.
 */
export function SearchInput({
  value,
  onChange,
  placeholder,
  label = 'Search',
}: {
  value: string
  onChange: (next: string) => void
  placeholder?: string
  label?: string
}) {
  return (
    <div className="relative w-full sm:max-w-xs">
      <IconSearch className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-ink-400" />
      <input
        type="search"
        aria-label={label}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        className="field-input pl-10"
      />
    </div>
  )
}

/** A row of filter pills. */
export function FilterChips<T extends string>({
  options,
  value,
  onChange,
}: {
  options: { value: T; label: string }[]
  value: T
  onChange: (next: T) => void
}) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={value === option.value}
          onClick={() => onChange(option.value)}
          className={`chip ${value === option.value ? 'chip-active' : ''}`}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}

/**
 * A headline figure.
 *
 * The accent bar down the left edge carries the tone, which keeps the tile
 * itself on the same surface as every other panel instead of turning the top
 * of the dashboard into four blocks of flat colour.
 */
export function StatTile({
  label,
  value,
  hint,
  tone,
  icon,
  compact = false,
}: {
  label: string
  value: ReactNode
  hint: string
  tone: 'brand' | 'accent' | 'success' | 'warn'
  icon?: ReactNode

  // Set for a tile whose value is a phrase rather than a number, such as a
  // time range. At the full display size those wrap onto a second line and
  // make the tile taller than the ones beside it.
  compact?: boolean
}) {
  const tones = {
    brand: { bar: 'bg-brand-500', chip: 'bg-brand-soft text-brand-soft-fg' },
    accent: { bar: 'bg-accent-500', chip: 'bg-accent-soft text-accent-soft-fg' },
    success: { bar: 'bg-success', chip: 'bg-success-bg text-success-fg' },
    warn: { bar: 'bg-warn-fg', chip: 'bg-warn-bg text-warn-fg' },
  }[tone]

  return (
    <div className="card relative overflow-hidden p-5">
      <span className={`absolute inset-y-0 left-0 w-1 ${tones.bar}`} aria-hidden="true" />

      <div className="flex items-start justify-between gap-3 pl-2">
        <div className="min-w-0">
          {/* The label is given the height of two lines whether it needs them
              or not. A tile whose label wraps would otherwise push its figure
              lower than the tiles beside it, and a row of headline numbers
              that do not share a baseline is the first thing the eye picks
              up on the dashboard. */}
          <p className="flex min-h-8 items-start text-xs font-medium uppercase tracking-wide text-ink-500">
            {label}
          </p>
          <p
            className={`numeric mt-1 font-semibold leading-none text-ink-900 ${
              compact ? 'whitespace-nowrap text-[26px]' : 'text-[34px]'
            }`}
          >
            {value}
          </p>
          <p className="mt-2 text-xs text-ink-400">{hint}</p>
        </div>

        {icon && (
          <span
            className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-xl ${tones.chip}`}
          >
            {icon}
          </span>
        )}
      </div>
    </div>
  )
}

/** A label and value pair, used by the detail screens. */
export function DetailRow({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-wrap items-baseline justify-between gap-3 border-b border-line py-3 last:border-b-0">
      <dt className="text-sm text-ink-500">{label}</dt>
      <dd className="text-sm font-medium text-ink-900">{children}</dd>
    </div>
  )
}

/** Placeholder shown while a request is in flight. */
export function Loading({ label = 'Loading…' }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20 text-sm text-ink-500">
      <span
        className="h-7 w-7 animate-spin rounded-full border-2 border-ink-200 border-t-brand-500"
        aria-hidden="true"
      />
      <span>{label}</span>
    </div>
  )
}

/** Placeholder shown when a list has no rows. */
export function EmptyState({
  title,
  hint,
  action,
}: {
  title: string
  hint?: string
  action?: ReactNode
}) {
  return (
    <div className="px-6 py-16 text-center">
      <span
        className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-surface-2 text-ink-400"
        aria-hidden="true"
      >
        <IconSearch className="h-5 w-5" />
      </span>
      <p className="mt-4 text-sm font-semibold text-ink-800">{title}</p>
      {hint && <p className="mx-auto mt-1.5 max-w-sm text-sm text-ink-400">{hint}</p>}
      {action && <div className="mt-5 flex justify-center">{action}</div>}
    </div>
  )
}

/**
 * Message banner. Errors carry the wording the API sent back, so the reason a
 * request was refused is always the server's own explanation.
 */
export function Alert({
  kind,
  message,
  onDismiss,
}: {
  kind: 'error' | 'success' | 'info'
  message: string
  onDismiss?: () => void
}) {
  // Each banner uses the same status pair as the badges, so a failure looks
  // the same wherever it is reported and both themes are covered at once.
  const { styles, icon } = {
    error: { styles: 'bg-danger-bg text-danger-fg', icon: <IconAlert className="h-4 w-4" /> },
    success: { styles: 'bg-success-bg text-success-fg', icon: <IconCheck className="h-4 w-4" /> },
    info: { styles: 'bg-info-bg text-info-fg', icon: <IconInfo className="h-4 w-4" /> },
  }[kind]

  return (
    <div
      role={kind === 'error' ? 'alert' : 'status'}
      className={`mb-4 flex items-start gap-3 rounded-2xl px-4 py-3.5 text-sm ${styles}`}
    >
      <span className="mt-0.5 shrink-0 opacity-90">{icon}</span>
      <span className="min-w-0 flex-1">{message}</span>

      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          aria-label="Dismiss"
          className="-m-1 shrink-0 rounded-lg p-1 opacity-60 transition-opacity hover:opacity-100"
        >
          <IconClose className="h-4 w-4" />
        </button>
      )}
    </div>
  )
}

/* -------------------------------------------------------------------------- */
/* Modal dialog                                                               */
/* -------------------------------------------------------------------------- */

/**
 * Centred dialog used for the create and edit forms.
 */
export function Modal({
  title,
  description,
  isOpen,
  onClose,
  children,
  footer,
}: {
  title: string
  description?: string
  isOpen: boolean
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
}) {
  // Escape closes the dialog, which is what any keyboard user will try first.
  useEffect(() => {
    if (!isOpen) return

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') onClose()
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [isOpen, onClose])

  // The page behind a dialog must not scroll, or a trackpad gesture over the
  // backdrop moves the list instead of the form the reader is looking at.
  useEffect(() => {
    if (!isOpen) return

    const previous = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    return () => {
      document.body.style.overflow = previous
    }
  }, [isOpen])

  if (!isOpen) return null

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-label={title}
      className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-ink-900/40 p-4 backdrop-blur-sm sm:p-8 dark:bg-black/70"
    >
      <div
        className="w-full max-w-2xl rounded-2xl border border-line bg-surface"
        style={{ boxShadow: 'var(--shadow-overlay)' }}
      >
        <div className="flex items-start justify-between gap-4 border-b border-line px-5 py-4 sm:px-6">
          <div className="min-w-0">
            <h2 className="card-title">{title}</h2>
            {description && <p className="mt-1 text-xs text-ink-500">{description}</p>}
          </div>

          <button
            type="button"
            onClick={onClose}
            aria-label="Close"
            className="-m-1.5 shrink-0 rounded-lg p-1.5 text-ink-400 transition-colors hover:bg-ink-100 hover:text-ink-900"
          >
            <IconClose className="h-4 w-4" />
          </button>
        </div>

        <div className="px-5 py-5 sm:px-6">{children}</div>

        {footer && (
          <div className="flex flex-wrap justify-end gap-2 border-t border-line px-5 py-4 sm:px-6">
            {footer}
          </div>
        )}
      </div>
    </div>
  )
}

/* -------------------------------------------------------------------------- */
/* Formatting helpers                                                         */
/* -------------------------------------------------------------------------- */

/**
 * Renders a UTC timestamp from the API in the reader's local time.
 * The API stores and returns UTC throughout; conversion happens only here, at
 * the very edge of the system.
 */
export function formatDateTime(utc: string | null | undefined): string {
  if (!utc) return '—'

  const date = new Date(utc)
  if (Number.isNaN(date.getTime())) return '—'

  return date.toLocaleString(undefined, {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/** Renders just the date part of a UTC timestamp in local time. */
export function formatDate(utc: string | null | undefined): string {
  if (!utc) return '—'

  const date = new Date(utc)
  if (Number.isNaN(date.getTime())) return '—'

  return date.toLocaleDateString(undefined, { day: '2-digit', month: 'short', year: 'numeric' })
}
