// -----------------------------------------------------------------------------
// File        : pages/DashboardPage.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Operational overview for back-office officers and grid
//               operators. Every figure shown here is read from the Web API,
//               which computes the counts; nothing on this page is calculated
//               in the browser or hard coded.

// -----------------------------------------------------------------------------

import { useCallback, useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { dashboardApi } from '../api/resources'
import { ApiError } from '../api/client'
import { useAuth } from '../context/useAuth'
import {
  Alert,
  EmptyState,
  Loading,
  PageHeader,
  StatTile,
  StatusBadge,
  formatDateTime,
} from '../components/Ui'
import { IconBolt, IconClock, IconExchange, IconNode, IconRefresh } from '../components/Icons'
import type { OperatorDashboard } from '../types'

export default function DashboardPage() {
  const { user } = useAuth()
  const [data, setData] = useState<OperatorDashboard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  /**
   * Fetches the live figures from the API.
   */
  const load = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      setData(await dashboardApi.operator())
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not load the dashboard.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  return (
    <>
      <PageHeader
        eyebrow="Overview"
        title={`Welcome back, ${user?.fullName?.split(' ')[0] ?? 'there'}`}
        description="Live operational picture of the VoltLink microgrid network."
        actions={
          <button type="button" onClick={() => void load()} className="btn-secondary">
            <IconRefresh className="h-4 w-4" />
            Refresh
          </button>
        }
      />

      {error && <Alert kind="error" message={error} onDismiss={() => setError(null)} />}

      {isLoading && !data ? (
        <Loading label="Loading dashboard…" />
      ) : data ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <StatTile
              label="Awaiting approval"
              value={data.pendingCount}
              hint="Reservations needing a decision"
              tone="warn"
              icon={<IconClock className="h-[18px] w-[18px]" />}
            />
            <StatTile
              label="Approved upcoming"
              value={data.approvedFutureCount}
              hint="Confirmed future transfers"
              tone="brand"
              icon={<IconExchange className="h-[18px] w-[18px]" />}
            />
            <StatTile
              label="Completed today"
              value={data.completedTodayCount}
              hint="Energy transfers finalised"
              tone="success"
              icon={<IconBolt className="h-[18px] w-[18px]" />}
            />
            <StatTile
              label="Active nodes"
              value={data.activeStationCount}
              hint="Microgrid stations in service"
              tone="accent"
              icon={<IconNode className="h-[18px] w-[18px]" />}
            />
          </div>

          <div className="card mt-6">
            <div className="card-header">
              <div>
                <h2 className="card-title">Today&rsquo;s schedule</h2>
                <p className="mt-1 text-xs text-ink-500">
                  Open reservations due within the current day.
                </p>
              </div>
              <Link to="/reservations" className="btn-secondary btn-sm">
                View all
              </Link>
            </div>

            {data.todaySchedule.length === 0 ? (
              <EmptyState
                title="Nothing scheduled for today"
                hint="Approved and pending reservations due today will appear here."
              />
            ) : (
              <div className="table-wrap">
                <table className="table">
                  <thead>
                    <tr>
                      <th>Reference</th>
                      <th>Prosumer NIC</th>
                      <th>Station</th>
                      <th>Window</th>
                      <th>Energy</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.todaySchedule.map((r) => (
                      <tr key={r.id}>
                        <td className="font-medium text-ink-900">
                          <Link
                            to={`/reservations/${r.id}`}
                            className="transition-colors hover:text-brand-600"
                          >
                            {r.reservationNo}
                          </Link>
                        </td>
                        <td>{r.prosumerNic}</td>
                        <td>{r.stationName ?? '—'}</td>
                        <td className="whitespace-nowrap">
                          {formatDateTime(r.reservationStartUtc)}
                        </td>
                        <td className="numeric whitespace-nowrap">{r.energyKwh} kWh</td>
                        <td>
                          <StatusBadge status={r.status} />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      ) : null}
    </>
  )
}
