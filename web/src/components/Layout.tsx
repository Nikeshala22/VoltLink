// -----------------------------------------------------------------------------
// File        : components/Layout.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Application shell: the branded navigation panel, the links
//               filtered by the signed in role, the theme switch and the
//               account menu.
//
//               The panel floats inside the page rather than running edge to
//               edge, which keeps the ambient background visible around it and
//               lets the content sit on its own rounded surface. It is
//               permanent from the large breakpoint upwards and becomes a
//               slide over drawer below it, so the same markup works from a
//               phone to a desktop.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import { useEffect, useRef, useState } from 'react'
import type { ComponentType, SVGProps } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import ThemeToggle from './ThemeToggle'
import { RoleBadge } from './Ui'
import {
  IconClose,
  IconDashboard,
  IconExchange,
  IconInbox,
  IconLogout,
  IconMenu,
  IconNode,
  IconSolar,
  IconUsers,
  LogoMark,
} from './Icons'
import type { UserRole } from '../types'

interface NavItem {
  to: string
  label: string
  Icon: ComponentType<SVGProps<SVGSVGElement>>

  // Which roles may see the link. The API enforces the same restriction, so
  // hiding a link is a convenience, never the actual protection.
  roles: UserRole[]
}

interface NavGroup {
  heading: string
  items: NavItem[]
}

// Grouping separates the day to day operational screens from the account
// administration ones, which only a back-office officer ever sees.
const NAV_GROUPS: NavGroup[] = [
  {
    heading: 'Operations',
    items: [
      {
        to: '/dashboard',
        label: 'Dashboard',
        Icon: IconDashboard,
        roles: ['Backoffice', 'GridOperator'],
      },
      {
        to: '/stations',
        label: 'Microgrid Nodes',
        Icon: IconNode,
        roles: ['Backoffice', 'GridOperator'],
      },
      {
        to: '/reservations',
        label: 'Reservations',
        Icon: IconExchange,
        roles: ['Backoffice', 'GridOperator'],
      },
      {
        to: '/prosumers',
        label: 'Prosumers',
        Icon: IconSolar,
        roles: ['Backoffice', 'GridOperator'],
      },
    ],
  },
  {
    heading: 'Administration',
    items: [
      { to: '/activations', label: 'Pending Activations', Icon: IconInbox, roles: ['Backoffice'] },
      { to: '/users', label: 'System Users', Icon: IconUsers, roles: ['Backoffice'] },
    ],
  },
]

/** Turns a full name into up to two initials, standing in for an avatar. */
function initialsOf(fullName: string | undefined): string {
  return (fullName ?? '')
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('')
}

export default function Layout() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const [isDrawerOpen, setIsDrawerOpen] = useState(false)
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)

  // Close the drawer whenever the route changes, so tapping a link on a phone
  // does not leave the panel covering the page that just loaded.
  useEffect(() => {
    setIsDrawerOpen(false)
    setIsMenuOpen(false)
  }, [location.pathname])

  // Escape closes whichever overlay is open, which is what any keyboard user
  // will try first.
  useEffect(() => {
    if (!isDrawerOpen && !isMenuOpen) return

    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      setIsDrawerOpen(false)
      setIsMenuOpen(false)
    }

    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [isDrawerOpen, isMenuOpen])

  // A click anywhere outside the account menu dismisses it, which is how every
  // other menu on the platform behaves.
  useEffect(() => {
    if (!isMenuOpen) return

    const onPointerDown = (event: PointerEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsMenuOpen(false)
      }
    }

    document.addEventListener('pointerdown', onPointerDown)
    return () => document.removeEventListener('pointerdown', onPointerDown)
  }, [isMenuOpen])

  function handleLogout() {
    logout()
    navigate('/login', { replace: true })
  }

  // Only the groups that still hold a link this role may use are drawn, so an
  // operator never sees an empty "Administration" heading.
  const groups = NAV_GROUPS.map((group) => ({
    ...group,
    items: group.items.filter((item) => user && item.roles.includes(user.role)),
  })).filter((group) => group.items.length > 0)

  const initials = initialsOf(user?.fullName)
  const roleLabel = user?.role === 'GridOperator' ? 'Grid Operator' : (user?.role ?? '')

  // The heading in the top bar names the screen currently on show, which the
  // route table alone cannot tell the header.
  const currentLabel =
    NAV_GROUPS.flatMap((group) => group.items).find((item) =>
      location.pathname.startsWith(item.to),
    )?.label ?? 'Console'

  return (
    <div className="aurora flex min-h-full">
      {/* Dimmed backdrop, only present while the drawer is open on small screens. */}
      {isDrawerOpen && (
        <button
          type="button"
          aria-label="Close navigation"
          onClick={() => setIsDrawerOpen(false)}
          className="fixed inset-0 z-30 bg-ink-900/40 backdrop-blur-sm lg:hidden dark:bg-black/70"
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-[17rem] flex-col
                    border-r border-line bg-surface
                    transition-transform duration-200 ease-out
                    lg:sticky lg:top-0 lg:h-screen lg:translate-x-0
                    ${isDrawerOpen ? 'translate-x-0' : '-translate-x-full'}`}
      >
        <div className="flex h-[4.5rem] shrink-0 items-center gap-3 px-5">
          <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-600 text-brand-fg">
            <LogoMark className="h-[22px] w-[22px]" />
          </span>

          <div className="min-w-0 flex-1">
            <p className="truncate text-[15px] font-semibold tracking-tight text-ink-900">
              VoltLink
            </p>
            <p className="truncate text-[11px] text-ink-400">Microgrid Console</p>
          </div>

          {/* Only useful while the panel is a drawer; the permanent panel has
              nothing to close. */}
          <button
            type="button"
            onClick={() => setIsDrawerOpen(false)}
            aria-label="Close navigation"
            className="btn-ghost -mr-2 h-9 w-9 rounded-lg p-0 lg:hidden"
          >
            <IconClose className="h-4 w-4" />
          </button>
        </div>

        <nav className="flex-1 space-y-6 overflow-y-auto px-3 pb-4">
          {groups.map((group) => (
            <div key={group.heading}>
              <p className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-[0.16em] text-ink-400">
                {group.heading}
              </p>

              <div className="space-y-0.5">
                {group.items.map((item) => (
                  <NavLink
                    key={item.to}
                    to={item.to}
                    className={({ isActive }) =>
                      `group relative flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm
                       transition-colors duration-150 ${
                         isActive
                           ? 'bg-brand-soft font-medium text-brand-soft-fg'
                           : 'text-ink-600 hover:bg-surface-2 hover:text-ink-900'
                       }`
                    }
                  >
                    {({ isActive }) => (
                      <>
                        {/* A short bar on the active row, so the current screen
                            is readable without relying on colour alone. */}
                        <span
                          aria-hidden="true"
                          className={`absolute left-0 top-1/2 h-5 w-[3px] -translate-y-1/2 rounded-r-full bg-brand-500 transition-opacity ${
                            isActive ? 'opacity-100' : 'opacity-0'
                          }`}
                        />
                        <item.Icon
                          className={`h-[18px] w-[18px] shrink-0 ${
                            isActive ? 'text-brand-600' : 'text-ink-400 group-hover:text-ink-600'
                          }`}
                        />
                        <span className="truncate">{item.label}</span>
                      </>
                    )}
                  </NavLink>
                ))}
              </div>
            </div>
          ))}
        </nav>

        {user && (
          <div className="shrink-0 border-t border-line p-3">
            <div className="flex items-center gap-3 rounded-xl bg-surface-2 px-3 py-2.5">
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-brand-600 text-xs font-semibold text-brand-fg">
                {initials || '?'}
              </span>
              <div className="min-w-0">
                <p className="truncate text-sm font-medium text-ink-900">{user.fullName}</p>
                <p className="truncate text-[11px] text-ink-400">{roleLabel}</p>
              </div>
            </div>
          </div>
        )}
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-20 flex h-[4.5rem] shrink-0 items-center gap-3 border-b border-line bg-canvas/80 px-4 backdrop-blur-xl sm:px-6">
          <button
            type="button"
            onClick={() => setIsDrawerOpen(true)}
            aria-label="Open navigation"
            className="btn-ghost -ml-2 h-10 w-10 rounded-xl p-0 lg:hidden"
          >
            <IconMenu className="h-5 w-5" />
          </button>

          <div className="min-w-0">
            <p className="text-[10px] font-semibold uppercase tracking-[0.16em] text-ink-400">
              VoltLink
            </p>
            <p className="truncate text-sm font-medium text-ink-800">{currentLabel}</p>
          </div>

          <div className="ml-auto flex items-center gap-2 sm:gap-3">
            <ThemeToggle />

            {user && (
              <div ref={menuRef} className="relative">
                <button
                  type="button"
                  onClick={() => setIsMenuOpen((open) => !open)}
                  aria-haspopup="menu"
                  aria-expanded={isMenuOpen}
                  className={`flex items-center gap-2.5 rounded-xl border py-1.5 pl-1.5 pr-2.5 transition-colors ${
                    isMenuOpen
                      ? 'border-line-strong bg-surface'
                      : 'border-transparent hover:border-line hover:bg-surface'
                  }`}
                >
                  <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-brand-soft text-xs font-semibold text-brand-soft-fg">
                    {initials || '?'}
                  </span>
                  <span className="hidden min-w-0 max-w-[10rem] text-left sm:block">
                    <span className="block truncate text-sm font-medium text-ink-900">
                      {user.fullName}
                    </span>
                    <span className="block truncate text-[11px] text-ink-400">{roleLabel}</span>
                  </span>
                </button>

                {isMenuOpen && (
                  <div
                    role="menu"
                    className="absolute right-0 top-full z-30 mt-2 w-64 rounded-2xl border border-line bg-surface p-1.5"
                    style={{ boxShadow: 'var(--shadow-raised)' }}
                  >
                    <div className="border-b border-line px-3 py-3">
                      <p className="truncate text-sm font-medium text-ink-900">{user.fullName}</p>
                      <p className="mt-0.5 truncate text-xs text-ink-500">{user.email}</p>
                      <div className="mt-2.5">
                        <RoleBadge role={user.role} />
                      </div>
                    </div>

                    <button
                      type="button"
                      role="menuitem"
                      onClick={handleLogout}
                      className="mt-1.5 flex w-full items-center gap-2.5 rounded-xl px-3 py-2.5 text-sm font-medium text-ink-700 transition-colors hover:bg-danger-bg hover:text-danger-fg"
                    >
                      <IconLogout className="h-4 w-4" />
                      Sign out
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        </header>

        <main className="flex-1 px-4 py-6 sm:px-6 sm:py-8">
          {/* Capped so long tables stay readable on a very wide monitor. */}
          <div className="mx-auto w-full max-w-[88rem]">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
