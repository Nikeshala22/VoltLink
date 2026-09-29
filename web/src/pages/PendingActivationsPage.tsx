// -----------------------------------------------------------------------------
// File        : pages/PendingActivationsPage.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : The pending activation view required by the specification.
//               Prosumers who register from the mobile application arrive
//               inactive and cannot sign in until a back-office officer
//               approves them here. Outstanding account closure requests are
//               shown alongside, because only this role may act on them.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import { useCallback, useEffect, useState } from 'react'
import { prosumersApi } from '../api/resources'
import { ApiError } from '../api/client'
import {
  Alert,
  EmptyState,
  Loading,
  PageHeader,
  SearchInput,
  StatTile,
  formatDateTime,
} from '../components/Ui'
import { IconClock, IconInbox, IconRefresh } from '../components/Icons'
import type { User } from '../types'

export default function PendingActivationsPage() {
  const [pending, setPending] = useState<User[]>([])
  const [closureRequests, setClosureRequests] = useState<User[]>([])
  const [search, setSearch] = useState('')

  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busyId, setBusyId] = useState<string | null>(null)

  /**
   * Loads both queues together, since a back-office officer works through them
   * at the same time.
   */
  const load = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      const [pendingResult, closureResult] = await Promise.all([
        prosumersApi.pending(),
        prosumersApi.deactivationRequests(),
      ])

      setPending(pendingResult)
      setClosureRequests(closureResult)
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not load the activation queue.')
    } finally {
      setIsLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  /**
   * Approves a registration so the prosumer can sign in on the mobile app.
   */
  async function handleActivate(prosumer: User) {
    setBusyId(prosumer.id)
    setError(null)
    setNotice(null)

    try {
      await prosumersApi.activate(prosumer.id)
      setNotice(`${prosumer.fullName} was activated and can now sign in.`)
      await load()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not activate the account.')
    } finally {
      setBusyId(null)
    }
  }

  /**
   * Carries out an account closure the prosumer asked for.
   */
  async function handleDeactivate(prosumer: User) {
    setBusyId(prosumer.id)
    setError(null)
    setNotice(null)

    try {
      await prosumersApi.deactivate(prosumer.id)
      setNotice(`${prosumer.fullName} was deactivated as requested.`)
      await load()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not deactivate the account.')
    } finally {
      setBusyId(null)
    }
  }

  /**
   * Dismisses a closure request, keeping the prosumer account active.
   */
  async function handleDismissClosure(prosumer: User) {
    setBusyId(prosumer.id)
    setError(null)
    setNotice(null)

    try {
      await prosumersApi.activate(prosumer.id)
      setNotice(`Closure request for ${prosumer.fullName} was dismissed. Account remains active.`)
      await load()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not update the account.')
    } finally {
      setBusyId(null)
    }
  }

  const query = search.trim().toLowerCase()
  const filteredPending = pending.filter(
    (p) =>
      !query ||
      p.id.toLowerCase().includes(query) ||
      p.fullName.toLowerCase().includes(query) ||
      p.email.toLowerCase().includes(query) ||
      (p.phone && p.phone.toLowerCase().includes(query)),
  )

  const filteredClosures = closureRequests.filter(
    (p) =>
      !query ||
      p.id.toLowerCase().includes(query) ||
      p.fullName.toLowerCase().includes(query) ||
      p.email.toLowerCase().includes(query),
  )

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Pending Activations"
        description="Mobile registrations awaiting approval, and account closure requests."
        actions={
          <button type="button" onClick={() => void load()} className="btn-secondary">
            <IconRefresh className="h-4 w-4" />
            Refresh
          </button>
        }
      />

      {error && <Alert kind="error" message={error} onDismiss={() => setError(null)} />}
      {notice && <Alert kind="success" message={notice} onDismiss={() => setNotice(null)} />}

      {isLoading ? (
        <Loading />
      ) : (
        <div className="space-y-6">
          <div className="grid gap-4 sm:grid-cols-2">
            <StatTile
              label="Awaiting Approval"
              value={pending.length}
              hint="New mobile applicants awaiting verification"
              tone="warn"
              icon={<IconClock className="h-[18px] w-[18px]" />}
            />
            <StatTile
              label="Closure Requests"
              value={closureRequests.length}
              hint="Prosumers requesting voluntary account closure"
              tone="brand"
              icon={<IconInbox className="h-[18px] w-[18px]" />}
            />
          </div>

          <div className="card">
            <div className="card-header flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
              <div>
                <h2 className="card-title">Awaiting activation</h2>
                <p className="mt-0.5 text-xs text-ink-500">
                  These prosumers registered on the mobile app and cannot sign in yet.
                </p>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <SearchInput
                  value={search}
                  onChange={setSearch}
                  placeholder="Filter by NIC, name or email…"
                  label="Search pending queues"
                />
                <span className="badge bg-warn-bg text-warn-fg">{pending.length} waiting</span>
              </div>
            </div>

            {filteredPending.length === 0 ? (
              <EmptyState
                title={search ? 'No matching applicants' : 'Nothing awaiting activation'}
                hint={search ? 'Try clearing your search query.' : 'New mobile registrations will appear here.'}
              />
            ) : (
              <div className="table-wrap">
                <table className="table">
                  <thead>
                    <tr>
                      <th>NIC</th>
                      <th>Name</th>
                      <th>Email</th>
                      <th>Phone</th>
                      <th>Registered</th>
                      <th className="text-right">Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredPending.map((p) => (
                      <tr key={p.id}>
                        <td className="font-mono text-xs font-medium text-ink-900">{p.id}</td>
                        <td className="font-medium text-ink-900">{p.fullName}</td>
                        <td className="text-xs">{p.email}</td>
                        <td className="text-xs">{p.phone ?? '—'}</td>
                        <td className="whitespace-nowrap text-xs">
                          {formatDateTime(p.createdAtUtc)}
                        </td>
                        <td className="text-right">
                          <button
                            type="button"
                            disabled={busyId === p.id}
                            onClick={() => void handleActivate(p)}
                            className="btn-success btn-sm"
                          >
                            {busyId === p.id ? 'Activating…' : 'Activate'}
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <div className="card">
            <div className="card-header">
              <div>
                <h2 className="card-title">Account closure requests</h2>
                <p className="mt-0.5 text-xs text-ink-500">
                  Prosumers who asked to close their account. The account stays usable until
                  a back-office officer acts.
                </p>
              </div>
              <span className="badge bg-neutral-bg text-neutral-fg">{closureRequests.length} open</span>
            </div>

            {filteredClosures.length === 0 ? (
              <EmptyState title={search ? 'No matching closure requests' : 'No closure requests'} />
            ) : (
              <div className="table-wrap">
                <table className="table">
                  <thead>
                    <tr>
                      <th>NIC</th>
                      <th>Name</th>
                      <th>Email</th>
                      <th className="text-right">Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {filteredClosures.map((p) => (
                      <tr key={p.id}>
                        <td className="font-mono text-xs font-medium text-ink-900">{p.id}</td>
                        <td className="font-medium text-ink-900">{p.fullName}</td>
                        <td className="text-xs">{p.email}</td>
                        <td className="text-right">
                          <div className="flex justify-end gap-2">
                            <button
                              type="button"
                              disabled={busyId === p.id}
                              onClick={() => void handleDismissClosure(p)}
                              className="btn-secondary btn-sm"
                            >
                              Keep active
                            </button>
                            <button
                              type="button"
                              disabled={busyId === p.id}
                              onClick={() => void handleDeactivate(p)}
                              className="btn-danger btn-sm"
                            >
                              {busyId === p.id ? 'Working…' : 'Deactivate'}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  )
}
