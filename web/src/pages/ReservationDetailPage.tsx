// -----------------------------------------------------------------------------
// File        : pages/ReservationDetailPage.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Full record of a single booking, its lifecycle timestamps and
//               the actions still available on it. Also carries the operator
//               tool for verifying a scanned prosumer QR code against the
//               service and finalising the energy transfer.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import { useCallback, useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import { reservationsApi } from '../api/resources'
import { ApiError } from '../api/client'
import {
  Alert,
  BackLink,
  DetailRow,
  Loading,
  PageHeader,
  StatusBadge,
  formatDateTime,
} from '../components/Ui'
import { IconCheck } from '../components/Icons'
import type { Reservation } from '../types'

export default function ReservationDetailPage() {
  const { id = '' } = useParams()

  const [reservation, setReservation] = useState<Reservation | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  /**
   * Loads the booking.
   */
  const load = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      setReservation(await reservationsApi.get(id))
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not load the reservation.')
    } finally {
      setIsLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  /**
   * Runs one of the lifecycle actions and reports the service's response.
   */
  async function runAction(action: 'approve' | 'reject' | 'cancel' | 'complete') {
    if (!reservation) return

    setIsBusy(true)
    setError(null)
    setNotice(null)

    try {
      if (action === 'approve') {
        await reservationsApi.approve(reservation.id)
        setNotice('Booking approved. A transaction QR code has been issued to the prosumer.')
      } else if (action === 'reject') {
        await reservationsApi.reject(reservation.id)
        setNotice('Booking rejected and its place released.')
      } else if (action === 'cancel') {
        setNotice((await reservationsApi.cancel(reservation.id)).message)
      } else {
        // Completing is what an operator does after scanning the QR code at
        // the station. The service refuses a second attempt.
        setNotice((await reservationsApi.complete(reservation.id)).message)
      }

      await load()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not complete the action.')
    } finally {
      setIsBusy(false)
    }
  }

  if (isLoading && !reservation) return <Loading label="Loading reservation…" />

  if (!reservation) {
    return (
      <>
        <BackLink to="/reservations">Back to reservations</BackLink>
        <PageHeader title="Reservation" />
        {error && <Alert kind="error" message={error} />}
      </>
    )
  }

  return (
    <>
      <BackLink to="/reservations">Back to reservations</BackLink>

      <PageHeader
        eyebrow="Reservation"
        title={reservation.reservationNo}
        description="Full record of this energy trading reservation."
        crumbs={[
          { label: 'Reservations', to: '/reservations' },
          { label: reservation.reservationNo },
        ]}
        actions={<StatusBadge status={reservation.status} />}
      />

      {error && <Alert kind="error" message={error} onDismiss={() => setError(null)} />}
      {notice && <Alert kind="success" message={notice} onDismiss={() => setNotice(null)} />}

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="card lg:col-span-2">
          <div className="card-header">
            <h2 className="card-title">Booking details</h2>
            <StatusBadge status={reservation.status} />
          </div>

          <div className="px-5 py-2">
            <DetailRow label="Prosumer">
              {reservation.prosumerName ?? '—'}
              <span className="ml-2 text-xs font-normal text-ink-400">
                {reservation.prosumerNic}
              </span>
            </DetailRow>
            <DetailRow label="Station">{reservation.stationName ?? '—'}</DetailRow>
            <DetailRow label="Transfer type">{reservation.type}</DetailRow>
            <DetailRow label="Energy">
              <span className="numeric">{reservation.energyKwh} kWh</span>
            </DetailRow>
            <DetailRow label="Window starts">
              {formatDateTime(reservation.reservationStartUtc)}
            </DetailRow>
            <DetailRow label="Window ends">
              {formatDateTime(reservation.reservationEndUtc)}
            </DetailRow>
            <DetailRow label="Requested">{formatDateTime(reservation.createdAtUtc)}</DetailRow>

            {reservation.cancelledAtUtc && (
              <DetailRow label="Cancelled">{formatDateTime(reservation.cancelledAtUtc)}</DetailRow>
            )}

            {reservation.completedAtUtc && (
              <DetailRow label="Completed">{formatDateTime(reservation.completedAtUtc)}</DetailRow>
            )}

            <DetailRow label="Transaction QR issued">
              {reservation.hasQrCode ? 'Yes' : 'No'}
            </DetailRow>
          </div>
        </div>

        <div className="space-y-6">
          <div className="card">
            <div className="card-header">
              <h2 className="card-title">Actions</h2>
            </div>

            <div className="space-y-2 p-5">
              {reservation.status === 'Pending' && (
                <>
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => void runAction('approve')}
                    className="btn-success w-full"
                  >
                    Approve booking
                  </button>
                  <button
                    type="button"
                    disabled={isBusy}
                    onClick={() => void runAction('reject')}
                    className="btn-secondary w-full"
                  >
                    Reject booking
                  </button>
                </>
              )}

              {reservation.status === 'Approved' && (
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() => void runAction('complete')}
                  className="btn-primary w-full"
                >
                  Mark energy transfer done
                </button>
              )}

              {/* Availability comes from the service, which applies the twelve
                  hour notice rule. */}
              {reservation.canBeCancelled ? (
                <button
                  type="button"
                  disabled={isBusy}
                  onClick={() => void runAction('cancel')}
                  className="btn-danger w-full"
                >
                  Cancel booking
                </button>
              ) : (
                reservation.status === 'Pending' || reservation.status === 'Approved' ? (
                  <p className="rounded-lg bg-surface-2 px-3 py-2 text-xs text-ink-500">
                    This booking can no longer be changed or cancelled: the service requires
                    at least 12 hours notice before the window starts.
                  </p>
                ) : null
              )}

              {['Completed', 'Cancelled', 'Rejected'].includes(reservation.status) && (
                <p className="rounded-lg bg-surface-2 px-3 py-2 text-xs text-ink-500">
                  This booking is closed and no further action is available.
                </p>
              )}
            </div>
          </div>

          <QrVerificationPanel onVerified={() => void load()} />
        </div>
      </div>
    </>
  )
}

/**
 * Operator tool for checking a scanned prosumer QR code.
 *
 * The token is sent to the service, which validates its signature and the state
 * of the booking. Nothing about the code is trusted on this side.
 */
function QrVerificationPanel({ onVerified }: { onVerified: () => void }) {
  const [token, setToken] = useState('')
  const [result, setResult] = useState<Reservation | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isBusy, setIsBusy] = useState(false)

  /**
   * Sends the scanned token to the service for verification.
   */
  async function handleVerify() {
    setIsBusy(true)
    setError(null)
    setResult(null)

    try {
      setResult(await reservationsApi.verifyQr(token.trim()))
    } catch (caught) {
      // Covers a forged code, one that has been superseded, and one that has
      // already been used, each with the service's own message.
      setError(caught instanceof ApiError ? caught.message : 'Could not verify the QR code.')
    } finally {
      setIsBusy(false)
    }
  }

  /**
   * Finalises the transfer for the booking the scan identified.
   */
  async function handleComplete() {
    if (!result) return

    setIsBusy(true)
    setError(null)

    try {
      await reservationsApi.complete(result.id)
      setResult(null)
      setToken('')
      onVerified()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not complete the transfer.')
    } finally {
      setIsBusy(false)
    }
  }

  return (
    <div className="card">
      <div className="card-header">
        <div>
          <h2 className="card-title">Verify QR code</h2>
          <p className="mt-0.5 text-xs text-ink-500">Checked against the service.</p>
        </div>
      </div>

      <div className="space-y-3 p-5">
        {error && <Alert kind="error" message={error} onDismiss={() => setError(null)} />}

        <textarea
          rows={3}
          value={token}
          onChange={(e) => setToken(e.target.value)}
          placeholder="Paste the scanned token…"
          className="field-input font-mono text-xs"
        />

        <button
          type="button"
          disabled={isBusy || token.trim().length === 0}
          onClick={() => void handleVerify()}
          className="btn-secondary w-full"
        >
          {isBusy ? 'Checking…' : 'Verify token'}
        </button>

        {result && (
          /* The status pair is used rather than a literal green, so the panel
             stays readable when the dark theme is on. */
          <div className="rounded-xl bg-success-bg p-3.5 text-success-fg">
            <p className="flex items-center gap-2 text-sm font-semibold">
              <IconCheck className="h-4 w-4" />
              {result.reservationNo}
            </p>
            <p className="mt-1.5 text-xs opacity-90">
              {result.prosumerName ?? result.prosumerNic} ·{' '}
              <span className="numeric">{result.energyKwh} kWh</span> ·{' '}
              {formatDateTime(result.reservationStartUtc)}
            </p>
            <button
              type="button"
              disabled={isBusy}
              onClick={() => void handleComplete()}
              className="btn-success btn-sm mt-3 w-full"
            >
              Finalise energy transfer
            </button>
          </div>
        )}
      </div>
    </div>
  )
}
