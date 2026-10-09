// -----------------------------------------------------------------------------
// File        : pages/VerifyOtpPage.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Enterprise Two-Factor Authentication (2FA) verification page.
//               Enforces time-based one-time password (OTP) verification for
//               privileged staff before granting system access.
// Author      : <IT Number - Member Name>
// Created     : 2026-10-09
// -----------------------------------------------------------------------------

import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { authApi } from '../api/resources'
import { ApiError } from '../api/client'
import { Alert } from '../components/Ui'
import { LogoMark } from '../components/Icons'

export default function VerifyOtpPage() {
  const location = useLocation()
  const navigate = useNavigate()
  const { completeLoginWith2Fa } = useAuth()

  // Extract email passed from LoginPage state
  const stateEmail = (location.state as { email?: string } | null)?.email || ''
  const [email, setEmail] = useState(stateEmail)
  const [otpCode, setOtpCode] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [resendCooldown, setResendCooldown] = useState(60)

  // Countdown timer for resend
  useEffect(() => {
    if (resendCooldown <= 0) return
    const timer = setInterval(() => {
      setResendCooldown((prev) => (prev > 0 ? prev - 1 : 0))
    }, 1000)
    return () => clearInterval(timer)
  }, [resendCooldown])

  async function handleVerify(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setNotice(null)

    const cleanedCode = otpCode.trim()
    if (cleanedCode.length !== 6) {
      setError('Please enter the complete 6-digit verification code.')
      return
    }

    if (!email) {
      setError('Missing email address. Please return to the sign-in page.')
      return
    }

    setIsSubmitting(true)

    try {
      const response = await authApi.verify2fa(email.trim(), cleanedCode)
      completeLoginWith2Fa(response)
      navigate('/dashboard', { replace: true })
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? caught.message
          : 'Invalid or expired verification code. Please try again.',
      )
      setIsSubmitting(false)
    }
  }

  function handleResend() {
    if (resendCooldown > 0) return
    setResendCooldown(60)
    setError(null)
    setNotice('A new 6-digit security code has been generated. (Demo Code: 123456)')
  }

  return (
    <div className="aurora flex min-h-full items-center justify-center p-4 sm:p-6">
      <div className="w-full max-w-md">
        <div
          className="overflow-hidden rounded-3xl border border-line bg-surface p-8 shadow-raised sm:p-10"
          style={{ boxShadow: 'var(--shadow-raised)' }}
        >
          {/* Header & Logo */}
          <div className="text-center">
            <span className="mx-auto inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-brand-600 text-brand-fg">
              <LogoMark className="h-6 w-6" />
            </span>
            <h1 className="mt-5 text-2xl font-semibold tracking-tight text-ink-900">
              Two-Factor Authentication
            </h1>
            <p className="mt-1.5 text-xs text-ink-500">
              Enhanced enterprise security requires second-factor authorization.
            </p>
          </div>

          {/* Target Email Banner */}
          <div className="mt-6 rounded-xl border border-line bg-surface-subtle p-3.5 text-center text-xs">
            <p className="text-ink-500">Security code sent to:</p>
            <p className="mt-0.5 font-mono font-semibold text-ink-900">{email || 'your registered account'}</p>
          </div>

          {/* Feedback messages */}
          {error && <div className="mt-4"><Alert kind="error" message={error} onDismiss={() => setError(null)} /></div>}
          {notice && <div className="mt-4"><Alert kind="success" message={notice} onDismiss={() => setNotice(null)} /></div>}

          {/* OTP Verification Form */}
          <form onSubmit={handleVerify} className="mt-6 space-y-5">
            <div>
              <label htmlFor="otp" className="field-label text-center">
                6-Digit Security Passcode
              </label>
              <input
                id="otp"
                type="text"
                required
                maxLength={6}
                inputMode="numeric"
                autoComplete="one-time-code"
                autoFocus
                value={otpCode}
                onChange={(e) => {
                  const cleaned = e.target.value.replace(/[^0-9]/g, '')
                  setOtpCode(cleaned)
                }}
                className="field-input text-center font-mono text-2xl tracking-[0.5em] font-bold"
                placeholder="••••••"
              />
            </div>

            {/* Demo Helper Banner */}
            <div className="rounded-lg border border-brand-soft bg-brand-soft/30 p-2.5 text-center text-[11px] text-ink-700">
              <p className="font-semibold text-brand-700">Enterprise Demo Mode</p>
              <p className="mt-0.5 text-ink-500">
                You can enter the generated OTP code or master demo code: <strong className="font-mono text-ink-900">123456</strong>
              </p>
            </div>

            <button
              type="submit"
              disabled={isSubmitting || otpCode.length !== 6}
              className="btn-primary w-full py-3 text-sm font-semibold"
            >
              {isSubmitting ? 'Verifying Security Token…' : 'Verify & Continue'}
            </button>
          </form>

          {/* Resend and Back to Login */}
          <div className="mt-6 flex flex-col items-center gap-3 text-xs">
            <button
              type="button"
              onClick={handleResend}
              disabled={resendCooldown > 0}
              className={`font-medium transition-colors ${
                resendCooldown > 0
                  ? 'cursor-not-allowed text-ink-400'
                  : 'text-brand-600 hover:text-brand-700 underline'
              }`}
            >
              {resendCooldown > 0
                ? `Resend code in ${resendCooldown}s`
                : 'Didn’t receive the code? Resend'}
            </button>

            <Link
              to="/login"
              className="font-medium text-ink-500 hover:text-ink-800"
            >
              ← Back to Sign In
            </Link>
          </div>
        </div>
      </div>
    </div>
  )
}
