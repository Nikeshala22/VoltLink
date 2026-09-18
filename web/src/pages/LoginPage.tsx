// -----------------------------------------------------------------------------
// File        : pages/LoginPage.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Sign in screen and the landing page of the web application.
//               The Web API decides whether the credentials are valid and
//               whether the account is active; this screen only shows the
//               outcome and routes the user according to the role it returns.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { ApiError } from '../api/client'
import { Alert } from '../components/Ui'
import { IconExchange, IconNode, IconSolar, LogoMark } from '../components/Icons'

// The three capabilities summarised on the showcase panel. Kept as data so the
// panel stays a loop rather than three near identical blocks of markup.
const HIGHLIGHTS = [
  {
    Icon: IconNode,
    title: 'Microgrid nodes',
    body: 'Register grid hubs, track battery storage and set operating hours.',
  },
  {
    Icon: IconExchange,
    title: 'Energy reservations',
    body: 'Approve, reschedule and finalise transfers across the network.',
  },
  {
    Icon: IconSolar,
    title: 'Prosumer accounts',
    body: 'Activate households that both generate and draw power.',
  },
]

export default function LoginPage() {
  const { user, login, isRestoring } = useAuth()
  const navigate = useNavigate()

  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Somebody already signed in has no reason to see this page.
  if (!isRestoring && user) {
    return <Navigate to="/dashboard" replace />
  }

  /**
   * Signs in, then routes by the role the API reports.
   */
  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const profile = await login(email.trim(), password)

      // A prosumer account belongs to the mobile application. Signing them out
      // again is clearer than dropping them into a back-office screen where
      // every request would be refused.
      if (profile.role === 'Prosumer') {
        setError(
          'Prosumer accounts are served by the VoltLink mobile application. ' +
            'Please sign in there instead.',
        )
        setIsSubmitting(false)
        return
      }

      navigate('/dashboard', { replace: true })
    } catch (caught) {
      // The API explains why a sign in failed, including an account that is
      // awaiting activation, so its wording is shown unchanged.
      setError(
        caught instanceof ApiError
          ? caught.message
          : 'Something went wrong while signing in. Please try again.',
      )
      setIsSubmitting(false)
    }
  }

  return (
    <div className="aurora flex min-h-full items-center justify-center p-4 sm:p-6">
      <div className="w-full max-w-5xl">
        <div className="grid overflow-hidden rounded-3xl border border-line bg-surface lg:grid-cols-[1.05fr_1fr]"
             style={{ boxShadow: 'var(--shadow-raised)' }}>
          {/* Showcase panel, hidden on small screens where the form matters more. */}
          <div className="relative hidden overflow-hidden bg-brand-700 p-10 text-white lg:flex lg:flex-col lg:justify-between">
            {/* A faint grid of connected nodes, drawn rather than downloaded.
                It reads as the network the console manages, and costs nothing
                beyond the markup already on the page. */}
            <svg
              aria-hidden="true"
              className="pointer-events-none absolute inset-0 h-full w-full opacity-[0.18]"
              viewBox="0 0 400 560"
              preserveAspectRatio="xMidYMid slice"
            >
              <defs>
                <pattern id="voltlink-grid" width="40" height="40" patternUnits="userSpaceOnUse">
                  <path d="M40 0H0V40" fill="none" stroke="white" strokeWidth="0.6" />
                </pattern>
              </defs>
              <rect width="400" height="560" fill="url(#voltlink-grid)" />
              <g stroke="white" strokeWidth="1.4" fill="none">
                <path d="M60 420 L140 300 L250 340 L330 210" />
                <path d="M140 300 L120 170 L250 120" />
              </g>
              <g fill="white">
                <circle cx="60" cy="420" r="5" />
                <circle cx="140" cy="300" r="7" />
                <circle cx="250" cy="340" r="5" />
                <circle cx="330" cy="210" r="5" />
                <circle cx="120" cy="170" r="5" />
                <circle cx="250" cy="120" r="7" />
              </g>
            </svg>

            <div className="relative">
              <span className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white/15 ring-1 ring-white/25">
                <LogoMark className="h-6 w-6" />
              </span>
              <h1 className="mt-7 text-4xl font-semibold tracking-tight">VoltLink</h1>
              <p className="mt-2 max-w-xs text-sm text-white/70">
                Smart Solar Microgrid Trading System
              </p>
            </div>

            <ul className="relative mt-10 space-y-5">
              {HIGHLIGHTS.map((highlight) => (
                <li key={highlight.title} className="flex gap-3.5">
                  <span className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-white/12 ring-1 ring-white/20">
                    <highlight.Icon className="h-[18px] w-[18px]" />
                  </span>
                  <div>
                    <p className="text-sm font-medium">{highlight.title}</p>
                    <p className="mt-0.5 text-xs leading-relaxed text-white/65">{highlight.body}</p>
                  </div>
                </li>
              ))}
            </ul>

            <p className="relative mt-10 text-xs text-white/50">
              Back-office and grid operator access only. Prosumers use the mobile app.
            </p>
          </div>

          <div className="p-8 sm:p-10 lg:p-12">
            {/* The mark is repeated here because the showcase panel beside it
                is hidden on a phone, where this would otherwise be an
                unbranded form. */}
            <span className="mb-7 inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-brand-600 text-brand-fg lg:hidden">
              <LogoMark className="h-6 w-6" />
            </span>

            <h2 className="text-2xl font-semibold tracking-tight text-ink-900">Sign in</h2>
            <p className="mt-1.5 mb-7 text-sm text-ink-500">
              Use your back-office or grid operator account to reach the console.
            </p>

            {error && <Alert kind="error" message={error} onDismiss={() => setError(null)} />}

            <form onSubmit={handleSubmit} className="space-y-5">
              <div>
                <label htmlFor="email" className="field-label">
                  Email address
                </label>
                <input
                  id="email"
                  type="email"
                  required
                  autoComplete="username"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  className="field-input"
                  placeholder="name@voltlink.lk"
                />
              </div>

              <div>
                <label htmlFor="password" className="field-label">
                  Password
                </label>
                <input
                  id="password"
                  type="password"
                  required
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="field-input"
                  placeholder="••••••••"
                />
              </div>

              <button type="submit" disabled={isSubmitting} className="btn-primary w-full py-3">
                {isSubmitting ? 'Signing in…' : 'Sign in'}
              </button>
            </form>

            <p className="mt-8 border-t border-line pt-5 text-xs text-ink-400">
              Prosumer accounts are served by the VoltLink mobile application.
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
