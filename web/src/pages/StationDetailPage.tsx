import { useCallback, useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { slotsApi, stationsApi } from '../api/resources'
import { ApiError } from '../api/client'
import {
  ActiveBadge,
  Alert,
  BackLink,
  EmptyState,
  Loading,
  Modal,
  PageHeader,
  StatTile,
  formatDateTime,
} from '../components/Ui'
import {
  IconBattery,
  IconBolt,
  IconClock,
  IconPin,
  IconPlus,
} from '../components/Icons'
import type { Slot, Station } from '../types'


function toLocalInputValue(utc: string): string {
  const date = new Date(utc)
  const offsetMs = date.getTimezoneOffset() * 60_000

  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16)
}


function toUtcIso(localValue: string): string {
  return new Date(localValue).toISOString()
}


function defaultSlotForm() {
  const start = new Date()
  start.setDate(start.getDate() + 1)
  start.setHours(9, 0, 0, 0)

  const end = new Date(start)
  end.setHours(start.getHours() + 2)

  return {
    startLocal: toLocalInputValue(start.toISOString()),
    endLocal: toLocalInputValue(end.toISOString()),
    capacity: 5,
    energyKwhPerSlot: 15,
    isActive: true,
  }
}

export default function StationDetailPage() {
  const { id = '' } = useParams()

  const [station, setStation] = useState<Station | null>(null)
  const [slots, setSlots] = useState<Slot[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editing, setEditing] = useState<Slot | null>(null)
  const [form, setForm] = useState(defaultSlotForm)
  const [isSaving, setIsSaving] = useState(false)

 
  const load = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      const [stationResult, slotResult] = await Promise.all([
        stationsApi.get(id),
        stationsApi.listSlots(id),
      ])

      setStation(stationResult)
      setSlots(slotResult)
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not load the station.')
    } finally {
      setIsLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load()
  }, [load])

  
  function openCreate() {
    setEditing(null)
    setForm(defaultSlotForm())
    setError(null)
    setIsFormOpen(true)
  }

  
  function openEdit(slot: Slot) {
    setEditing(slot)
    setForm({
      startLocal: toLocalInputValue(slot.startTimeUtc),
      endLocal: toLocalInputValue(slot.endTimeUtc),
      capacity: slot.capacity,
      energyKwhPerSlot: slot.energyKwhPerSlot,
      isActive: slot.isActive,
    })
    setError(null)
    setIsFormOpen(true)
  }


  async function handleSave(event: FormEvent) {
    event.preventDefault()
    setIsSaving(true)
    setError(null)

    const payload = {
      startTimeUtc: toUtcIso(form.startLocal),
      endTimeUtc: toUtcIso(form.endLocal),
      capacity: form.capacity,
      energyKwhPerSlot: form.energyKwhPerSlot,
      isActive: form.isActive,
    }

    try {
      if (editing) {
        await slotsApi.update(editing.id, payload)
        setNotice('Booking window updated.')
      } else {
        await stationsApi.createSlot(id, payload)
        setNotice('Booking window added.')
      }

      setIsFormOpen(false)
      await load()
    } catch (caught) {
      
      setError(caught instanceof ApiError ? caught.message : 'Could not save the booking window.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleDelete(slot: Slot) {
    setError(null)
    setNotice(null)

    try {
      await slotsApi.remove(slot.id)
      setNotice('Booking window deleted.')
      await load()
    } catch (caught) {
      setError(caught instanceof ApiError ? caught.message : 'Could not delete the booking window.')
    }
  }

  if (isLoading && !station) return <Loading label="Loading station…" />

  return (
    <>
      <BackLink to="/stations">Back to nodes</BackLink>

      <PageHeader
        eyebrow="Microgrid node"
        title={station ? `${station.name}` : 'Station'}
        description={
          station ? `${station.code} · ${station.addressLine}, ${station.city}` : undefined
        }
        crumbs={[
          { label: 'Microgrid Nodes', to: '/stations' },
          { label: station?.code ?? 'Station' },
        ]}
        actions={
          <button type="button" onClick={openCreate} className="btn-primary">
            <IconPlus className="h-4 w-4" />
            Add booking window
          </button>
        }
      />

      {error && <Alert kind="error" message={error} onDismiss={() => setError(null)} />}
      {notice && <Alert kind="success" message={notice} onDismiss={() => setNotice(null)} />}

      {station && (
        <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          <StatTile
            label="Capacity"
            value={`${station.capacityKwh} kWh`}
            hint="Rated throughput of this node"
            tone="accent"
            icon={<IconBolt className="h-4.5 w-4.5" />}
          />
          <StatTile
            label="Battery slots free"
            value={
              <>
                {station.availableBatterySlots}
                <span className="text-lg text-ink-400"> / {station.totalBatterySlots}</span>
              </>
            }
            hint="Storage bays currently available"
            tone="brand"
            icon={<IconBattery className="h-4.5 w-4.5" />}
          />
          <StatTile
            label="Operating hours"
            value={`${station.operatingHours.openTime}–${station.operatingHours.closeTime}`}
            hint="Window the node accepts transfers"
            compact
            tone="warn"
            icon={<IconClock className="h-4.5 w-4.5" />}
          />

      
          <div className="card relative overflow-hidden p-5">
            <span
              className={`absolute inset-y-0 left-0 w-1 ${station.isActive ? 'bg-success' : 'bg-ink-300'}`}
              aria-hidden="true"
            />
            <div className="flex items-start justify-between gap-3 pl-2">
              <div>
                <p className="text-xs font-medium uppercase tracking-wide text-ink-500">Status</p>
                <p className="mt-3">
                  <ActiveBadge isActive={station.isActive} />
                </p>
                <p className="numeric mt-3 text-xs text-ink-400">
                  {station.latitude.toFixed(4)}, {station.longitude.toFixed(4)}
                </p>
              </div>
              <span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-surface-2 text-ink-500">
                <IconPin className="h-4.5 w-4.5" />
              </span>
            </div>
          </div>
        </div>
      )}

      <div className="card">
        <div className="card-header">
          <div>
            <h2 className="card-title">Energy booking windows</h2>
            <p className="mt-0.5 text-xs text-ink-500">
              Prosumers reserve places in these windows from the mobile application.
            </p>
          </div>
        </div>

        {slots.length === 0 ? (
          <EmptyState
            title="No booking windows yet"
            hint="Add a window so prosumers can reserve energy transfers at this node."
          />
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Starts</th>
                  <th>Ends</th>
                  <th>Energy</th>
                  <th>Booked</th>
                  <th>Remaining</th>
                  <th>Status</th>
                  <th className="text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {slots.map((slot) => (
                  <tr key={slot.id}>
                    <td className="whitespace-nowrap font-medium text-ink-900">
                      {formatDateTime(slot.startTimeUtc)}
                    </td>
                    <td className="whitespace-nowrap">{formatDateTime(slot.endTimeUtc)}</td>
                    <td className="numeric whitespace-nowrap">{slot.energyKwhPerSlot} kWh</td>
                    <td>
                      {slot.bookedCount} / {slot.capacity}
                    </td>
                    <td>
                      <span
                        className={`badge ${
                          slot.remainingCapacity === 0
                            ? 'bg-danger-bg text-danger-fg'
                            : 'bg-success-bg text-success-fg'
                        }`}
                      >
                        {slot.remainingCapacity === 0 ? 'Full' : `${slot.remainingCapacity} free`}
                      </span>
                    </td>
                    <td>
                      <ActiveBadge isActive={slot.isActive} />
                    </td>
                    <td>
                      <div className="flex justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEdit(slot)}
                          className="btn-secondary btn-sm"
                        >
                          Edit
                        </button>
                        <button
                          type="button"
                          onClick={() => void handleDelete(slot)}
                          className="btn-danger btn-sm"
                        >
                          Delete
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

      <Modal
        title={editing ? 'Edit booking window' : 'Add booking window'}
        isOpen={isFormOpen}
        onClose={() => setIsFormOpen(false)}
        footer={
          <>
            <button type="button" onClick={() => setIsFormOpen(false)} className="btn-secondary">
              Cancel
            </button>
            <button type="submit" form="slot-form" disabled={isSaving} className="btn-primary">
              {isSaving ? 'Saving…' : 'Save window'}
            </button>
          </>
        }
      >
        <form id="slot-form" onSubmit={handleSave} className="grid gap-4 sm:grid-cols-2">
          <div>
            <label className="field-label">Starts</label>
            <input
              required
              type="datetime-local"
              value={form.startLocal}
              onChange={(e) => setForm({ ...form, startLocal: e.target.value })}
              className="field-input"
            />
          </div>

          <div>
            <label className="field-label">Ends</label>
            <input
              required
              type="datetime-local"
              value={form.endLocal}
              onChange={(e) => setForm({ ...form, endLocal: e.target.value })}
              className="field-input"
            />
          </div>

          <div>
            <label className="field-label">Places available</label>
            <input
              required
              type="number"
              min={1}
              value={form.capacity}
              onChange={(e) => setForm({ ...form, capacity: Number(e.target.value) })}
              className="field-input"
            />
          </div>

          <div>
            <label className="field-label">Energy per place (kWh)</label>
            <input
              required
              type="number"
              min={0.1}
              step="0.1"
              value={form.energyKwhPerSlot}
              onChange={(e) => setForm({ ...form, energyKwhPerSlot: Number(e.target.value) })}
              className="field-input"
            />
          </div>

          {editing && (
            <label className="flex items-center gap-2 text-sm sm:col-span-2">
              <input
                type="checkbox"
                checked={form.isActive}
                onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
                className="h-4 w-4 rounded border-line-strong"
              />
              Window is open for booking
            </label>
          )}

          <p className="text-xs text-ink-400 sm:col-span-2">
            Times are entered in your local time and sent to the service as UTC. A window
            must end after it starts and cannot be longer than 24 hours.
          </p>
        </form>
      </Modal>
    </>
  )
}
