// -----------------------------------------------------------------------------
// File        : pages/ProsumersPage.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Solar prosumer accounts, keyed by NIC. Staff search and review
//               the accounts; back-office officers create them, edit them and
//               control activation. Only a back-office officer can reactivate a
//               deactivated account, which the service enforces.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { Link } from "react-router-dom";
import { prosumersApi } from "../api/resources";
import type {
  CreateProsumerPayload,
  UpdateUserPayload,
} from "../api/resources";
import { ApiError } from "../api/client";
import { useAuth } from "../context/useAuth";
import {
  ActiveBadge,
  Alert,
  EmptyState,
  FilterChips,
  Loading,
  Modal,
  PageHeader,
  SearchInput,
  formatDate,
} from "../components/Ui";
import { IconInbox, IconPlus } from "../components/Icons";
import type { User } from "../types";

const BLANK_FORM: CreateProsumerPayload = {
  nic: "",
  fullName: "",
  email: "",
  phone: "",
  address: "",
  password: "",
  activateImmediately: true,
};

export default function ProsumersPage() {
  const { user } = useAuth();
  const isBackoffice = user?.role === "Backoffice";

  const [prosumers, setProsumers] = useState<User[]>([]);
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState<
    "all" | "active" | "inactive" | "closure"
  >("all");

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [form, setForm] = useState(BLANK_FORM);
  const [modalError, setModalError] = useState<string | null>(null);

  const [editingProsumer, setEditingProsumer] = useState<User | null>(null);
  const [editForm, setEditForm] = useState<UpdateUserPayload>({
    fullName: "",
    phone: "",
    address: "",
  });
  const [editModalError, setEditModalError] = useState<string | null>(null);
  const [inspectingProsumer, setInspectingProsumer] = useState<User | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  /**
   * Loads the prosumer list using the search query.
   */
  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      setProsumers(
        await prosumersApi.list({
          search: search.trim() || undefined,
        }),
      );
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? caught.message
          : "Could not load prosumers.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [search]);

  useEffect(() => {
    void load();
  }, [load]);

  /**
   * Registers a prosumer on behalf of a walk-in applicant.
   */
  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    setModalError(null);
    setError(null);

    const trimmedNic = form.nic.trim().toUpperCase();
    const trimmedName = form.fullName.trim();
    const trimmedEmail = form.email.trim();

    if (!trimmedNic || !/^[A-Z0-9]{5,20}$/.test(trimmedNic)) {
      setModalError("NIC must be between 5 and 20 alphanumeric characters (e.g. 199912345678 or 123456789V).");
      return;
    }
    if (trimmedName.length < 2) {
      setModalError("Full name must be at least 2 characters long.");
      return;
    }
    if (!trimmedEmail || !/^\S+@\S+\.\S+$/.test(trimmedEmail)) {
      setModalError("Please enter a valid email address.");
      return;
    }
    if (form.password.length < 6) {
      setModalError("Password must be at least 6 characters.");
      return;
    }

    setIsSaving(true);

    try {
      await prosumersApi.create({
        ...form,
        nic: trimmedNic,
        fullName: trimmedName,
        email: trimmedEmail,
        phone: form.phone.trim() || undefined,
        address: form.address.trim() || undefined,
      });
      setNotice(`Prosumer ${trimmedNic} was registered successfully.`);
      setIsFormOpen(false);
      setForm(BLANK_FORM);
      await load();
    } catch (caught) {
      // A NIC or email already in use or validation failure is reported here.
      const message =
        caught instanceof ApiError
          ? caught.message
          : "Could not register the prosumer.";
      setModalError(message);
      setError(message);
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Opens the edit modal populated with an existing prosumer's details.
   */
  function openEdit(prosumer: User) {
    setEditingProsumer(prosumer);
    setEditForm({
      fullName: prosumer.fullName,
      phone: prosumer.phone ?? "",
      address: prosumer.address ?? "",
    });
    setEditModalError(null);
    setError(null);
  }

  /**
   * Updates an existing prosumer's profile details.
   */
  async function handleUpdate(event: FormEvent) {
    event.preventDefault();
    if (!editingProsumer) return;

    const trimmedName = editForm.fullName.trim();
    if (trimmedName.length < 2) {
      setEditModalError("Full name must be at least 2 characters long.");
      return;
    }

    setIsSaving(true);
    setEditModalError(null);
    setError(null);

    try {
      await prosumersApi.update(editingProsumer.id, {
        ...editForm,
        fullName: trimmedName,
        phone: editForm.phone?.trim() || undefined,
        address: editForm.address?.trim() || undefined,
      });
      setNotice(`Prosumer ${editingProsumer.id} profile was updated successfully.`);
      setEditingProsumer(null);
      await load();
    } catch (caught) {
      const message =
        caught instanceof ApiError
          ? caught.message
          : "Could not update prosumer profile.";
      setEditModalError(message);
      setError(message);
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Activates or deactivates an account.
   */
  async function handleToggleActive(prosumer: User) {
    setError(null);
    setNotice(null);

    try {
      if (prosumer.isActive) {
        await prosumersApi.deactivate(prosumer.id);
        setNotice(`${prosumer.fullName} was deactivated.`);
      } else {
        await prosumersApi.activate(prosumer.id);
        setNotice(`${prosumer.fullName} was activated and can now sign in.`);
      }

      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? caught.message
          : "Could not change the account status.",
      );
    }
  }

  const counts = {
    all: prosumers.length,
    active: prosumers.filter((p) => p.isActive).length,
    inactive: prosumers.filter((p) => !p.isActive).length,
    closure: prosumers.filter((p) => p.deactivationRequested).length,
  };

  const displayedProsumers = prosumers.filter((p) => {
    if (activeFilter === "active") return p.isActive;
    if (activeFilter === "inactive") return !p.isActive;
    if (activeFilter === "closure") return p.deactivationRequested;
    return true;
  });

  return (
    <>
      <PageHeader
        eyebrow='Accounts'
        title='Solar Prosumers'
        description='Property owners trading energy with the microgrid, identified by NIC.'
        actions={
          isBackoffice ? (
            <div className='flex flex-wrap items-center gap-2'>
              <Link to='/activations' className='btn-secondary'>
                <IconInbox className='h-4 w-4' />
                Pending Activations
              </Link>
              <button
                type='button'
                onClick={() => {
                  setForm(BLANK_FORM);
                  setModalError(null);
                  setIsFormOpen(true);
                }}
                className='btn-primary'
              >
                <IconPlus className='h-4 w-4' />
                Register prosumer
              </button>
            </div>
          ) : undefined
        }
      />

      {error && (
        <Alert kind='error' message={error} onDismiss={() => setError(null)} />
      )}
      {notice && (
        <Alert
          kind='success'
          message={notice}
          onDismiss={() => setNotice(null)}
        />
      )}

      <div className='card'>
        <div className='card-header'>
          <FilterChips
            value={activeFilter}
            onChange={(next) =>
              setActiveFilter(next as "all" | "active" | "inactive" | "closure")
            }
            options={[
              { value: "all", label: `All (${counts.all})` },
              { value: "active", label: `Active (${counts.active})` },
              { value: "inactive", label: `Inactive (${counts.inactive})` },
              { value: "closure", label: `Closure Requests (${counts.closure})` },
            ]}
          />

          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder='Search NIC, name or email…'
            label='Search prosumers'
          />
        </div>

        {isLoading ? (
          <Loading />
        ) : displayedProsumers.length === 0 ? (
          <EmptyState
            title='No prosumers found'
            hint={
              search
                ? 'No prosumers match your search query.'
                : activeFilter === 'closure'
                ? 'No prosumers currently have an active closure request.'
                : 'Try a different filter or search term.'
            }
          />
        ) : (
          <>
            <ul className='divide-y divide-line md:hidden'>
              {displayedProsumers.map((prosumer) => (
                <li key={prosumer.id} className='p-4'>
                  <div className='flex items-start justify-between gap-3'>
                    <div className='min-w-0'>
                      <p className='font-medium text-ink-900'>
                        {prosumer.fullName}
                      </p>
                      <p className='font-mono text-xs text-ink-400'>
                        {prosumer.id}
                      </p>
                    </div>
                    <div className='flex shrink-0 flex-col items-end gap-1'>
                      <ActiveBadge isActive={prosumer.isActive} />
                      {prosumer.deactivationRequested && (
                        <span className='badge bg-warn-bg text-warn-fg'>
                          Closure requested
                        </span>
                      )}
                    </div>
                  </div>

                  <p className='mt-2 text-xs text-ink-500'>{prosumer.email}</p>
                  <p className='text-xs text-ink-400'>
                    {prosumer.phone ?? "—"}
                  </p>

                  <div className='mt-3 flex flex-wrap gap-2'>
                    <button
                      type='button'
                      onClick={() => setInspectingProsumer(prosumer)}
                      className='btn-secondary btn-sm'
                    >
                      View Details
                    </button>
                    {isBackoffice && (
                      <>
                        <button
                          type='button'
                          onClick={() => openEdit(prosumer)}
                          className='btn-secondary btn-sm'
                        >
                          Edit
                        </button>
                        <button
                          type='button'
                          onClick={() => void handleToggleActive(prosumer)}
                          className={
                            prosumer.isActive
                              ? "btn-danger btn-sm"
                              : "btn-success btn-sm"
                          }
                        >
                          {prosumer.isActive ? "Deactivate" : "Activate"}
                        </button>
                      </>
                    )}
                  </div>
                </li>
              ))}
            </ul>

            <div className='table-wrap hidden md:block'>
              <table className='table'>
                <thead>
                  <tr>
                    <th>NIC</th>
                    <th>Name</th>
                    <th>Contact</th>
                    <th>Registered</th>
                    <th>Status</th>
                    <th className='text-right'>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {displayedProsumers.map((p) => (
                    <tr key={p.id}>
                      <td className='font-mono text-xs font-medium text-ink-900'>
                        {p.id}
                      </td>
                      <td className='font-medium text-ink-900'>{p.fullName}</td>
                      <td>
                        <p className='text-xs'>{p.email}</p>
                        <p className='text-xs text-ink-400'>{p.phone ?? "—"}</p>
                      </td>
                      <td className='whitespace-nowrap text-xs'>
                        {formatDate(p.createdAtUtc)}
                      </td>
                      <td>
                        <div className='flex flex-wrap gap-1'>
                          <ActiveBadge isActive={p.isActive} />
                          {p.deactivationRequested && (
                            <span className='badge bg-warn-bg text-warn-fg'>
                              Closure requested
                            </span>
                          )}
                        </div>
                      </td>
                      <td>
                        <div className='flex justify-end gap-2'>
                          <button
                            type='button'
                            onClick={() => setInspectingProsumer(p)}
                            className='btn-secondary btn-sm'
                          >
                            View
                          </button>
                          {isBackoffice && (
                            <>
                              <button
                                type='button'
                                onClick={() => openEdit(p)}
                                className='btn-secondary btn-sm'
                              >
                                Edit
                              </button>
                              <button
                                type='button'
                                onClick={() => void handleToggleActive(p)}
                                className={
                                  p.isActive
                                    ? "btn-danger btn-sm"
                                    : "btn-success btn-sm"
                                }
                              >
                                {p.isActive ? "Deactivate" : "Activate"}
                              </button>
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>

      <Modal
        title='Register a solar prosumer'
        isOpen={isFormOpen}
        onClose={() => {
          setIsFormOpen(false);
          setModalError(null);
        }}
        footer={
          <>
            <button
              type='button'
              onClick={() => {
                setIsFormOpen(false);
                setModalError(null);
              }}
              className='btn-secondary'
            >
              Cancel
            </button>
            <button
              type='submit'
              form='prosumer-form'
              disabled={isSaving}
              className='btn-primary'
            >
              {isSaving ? "Saving…" : "Register prosumer"}
            </button>
          </>
        }
      >
        <div className='space-y-4'>
          {modalError && (
            <Alert
              kind='error'
              message={modalError}
              onDismiss={() => setModalError(null)}
            />
          )}

          <form
            id='prosumer-form'
            onSubmit={handleCreate}
            className='grid gap-4 sm:grid-cols-2'
          >
            <div>
              <label className='field-label'>NIC (primary key)</label>
              <input
                required
                value={form.nic}
                onChange={(e) =>
                  setForm({ ...form, nic: e.target.value.toUpperCase() })
                }
                className='field-input font-mono'
                placeholder='200145600789 or 123456789V'
                minLength={5}
                maxLength={20}
              />
              <p className='mt-1 text-xs text-ink-400'>
                5-20 alphanumeric characters (e.g. 199912345678 or 123456789V).
              </p>
            </div>

            <div>
              <label className='field-label'>Full name</label>
              <input
                required
                minLength={2}
                maxLength={120}
                value={form.fullName}
                onChange={(e) => setForm({ ...form, fullName: e.target.value })}
                className='field-input'
                placeholder='e.g. Kasun Perera'
              />
            </div>

            <div>
              <label className='field-label'>Email</label>
              <input
                required
                type='email'
                maxLength={120}
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                className='field-input'
                placeholder='kasun@gmail.com'
              />
            </div>

            <div>
              <label className='field-label'>Phone</label>
              <input
                type='tel'
                maxLength={20}
                value={form.phone}
                onChange={(e) => setForm({ ...form, phone: e.target.value })}
                className='field-input'
                placeholder='+94 77 123 4567'
              />
            </div>

            <div className='sm:col-span-2'>
              <label className='field-label'>Address</label>
              <input
                maxLength={200}
                value={form.address}
                onChange={(e) => setForm({ ...form, address: e.target.value })}
                className='field-input'
                placeholder='e.g. 45 Temple Road, Colombo 03'
              />
            </div>

            <div className='sm:col-span-2'>
              <label className='field-label'>Temporary password</label>
              <input
                required
                type='password'
                minLength={6}
                maxLength={100}
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                className='field-input'
                placeholder='At least 6 characters'
              />
            </div>

            <label className='flex items-center gap-2 text-sm sm:col-span-2'>
              <input
                type='checkbox'
                checked={form.activateImmediately}
                onChange={(e) =>
                  setForm({ ...form, activateImmediately: e.target.checked })
                }
                className='h-4 w-4 rounded border-line-strong'
              />
              Activate straight away
            </label>

            <p className='text-xs text-ink-400 sm:col-span-2'>
              Prosumers who register themselves from the mobile application always
              arrive inactive and appear under Pending Activations.
            </p>
          </form>
        </div>
      </Modal>

      <Modal
        title={`Edit Prosumer (${editingProsumer?.id ?? ""})`}
        isOpen={editingProsumer !== null}
        onClose={() => {
          setEditingProsumer(null);
          setEditModalError(null);
        }}
        footer={
          <>
            <button
              type='button'
              onClick={() => {
                setEditingProsumer(null);
                setEditModalError(null);
              }}
              className='btn-secondary'
            >
              Cancel
            </button>
            <button
              type='submit'
              form='edit-prosumer-form'
              disabled={isSaving}
              className='btn-primary'
            >
              {isSaving ? "Saving…" : "Save changes"}
            </button>
          </>
        }
      >
        <div className='space-y-4'>
          {editModalError && (
            <Alert
              kind='error'
              message={editModalError}
              onDismiss={() => setEditModalError(null)}
            />
          )}

          <form
            id='edit-prosumer-form'
            onSubmit={handleUpdate}
            className='space-y-4'
          >
            <div>
              <label className='field-label'>NIC (primary key)</label>
              <input
                disabled
                value={editingProsumer?.id ?? ""}
                className='field-input font-mono opacity-70'
              />
              <p className='mt-1 text-xs text-ink-400'>
                National Identity Card number is the primary key and cannot be
                changed.
              </p>
            </div>

            <div>
              <label className='field-label'>Email</label>
              <input
                disabled
                value={editingProsumer?.email ?? ""}
                className='field-input opacity-70'
              />
            </div>

            <div>
              <label className='field-label'>Full name</label>
              <input
                required
                minLength={2}
                maxLength={120}
                value={editForm.fullName}
                onChange={(e) =>
                  setEditForm({ ...editForm, fullName: e.target.value })
                }
                className='field-input'
              />
            </div>

            <div>
              <label className='field-label'>Phone</label>
              <input
                type='tel'
                maxLength={20}
                value={editForm.phone ?? ""}
                onChange={(e) =>
                  setEditForm({ ...editForm, phone: e.target.value })
                }
                className='field-input'
                placeholder='+94 77 123 4567'
              />
            </div>

            <div>
              <label className='field-label'>Property address</label>
              <input
                maxLength={200}
                value={editForm.address ?? ""}
                onChange={(e) =>
                  setEditForm({ ...editForm, address: e.target.value })
                }
                className='field-input'
                placeholder='Address with solar array installation'
              />
            </div>
          </form>
        </div>
      </Modal>

      {/* Prosumer Profile & Property Inspection Modal */}
      <Modal
        title={`Prosumer Profile — ${inspectingProsumer?.id ?? ""}`}
        isOpen={inspectingProsumer !== null}
        onClose={() => setInspectingProsumer(null)}
        footer={
          <div className='flex w-full items-center justify-between'>
            <div>
              {isBackoffice && inspectingProsumer && (
                <button
                  type='button'
                  onClick={() => {
                    const target = inspectingProsumer;
                    setInspectingProsumer(null);
                    openEdit(target);
                  }}
                  className='btn-secondary'
                >
                  Edit Profile
                </button>
              )}
            </div>
            <button
              type='button'
              onClick={() => setInspectingProsumer(null)}
              className='btn-primary'
            >
              Close
            </button>
          </div>
        }
      >
        {inspectingProsumer && (
          <div className='space-y-4 text-sm'>
            <div className='rounded-lg border border-line bg-surface-subtle p-4'>
              <div className='flex items-center justify-between'>
                <div>
                  <p className='text-xs font-semibold uppercase tracking-wider text-ink-400'>
                    National Identity Card (NIC)
                  </p>
                  <p className='font-mono text-base font-bold text-ink-900'>
                    {inspectingProsumer.id}
                  </p>
                </div>
                <div className='flex flex-wrap gap-1.5'>
                  <ActiveBadge isActive={inspectingProsumer.isActive} />
                  {inspectingProsumer.deactivationRequested && (
                    <span className='badge bg-warn-bg text-warn-fg'>
                      Closure requested
                    </span>
                  )}
                </div>
              </div>
            </div>

            <div className='grid gap-3 sm:grid-cols-2'>
              <div>
                <p className='text-xs text-ink-400'>Full Name</p>
                <p className='font-medium text-ink-900'>
                  {inspectingProsumer.fullName}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>System Role</p>
                <p className='font-medium text-ink-900'>
                  {inspectingProsumer.role}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>Email Address</p>
                <p className='font-medium text-ink-900'>
                  {inspectingProsumer.email}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>Contact Number</p>
                <p className='font-medium text-ink-900'>
                  {inspectingProsumer.phone || "Not provided"}
                </p>
              </div>
              <div className='sm:col-span-2'>
                <p className='text-xs text-ink-400'>Property Installation Address</p>
                <p className='font-medium text-ink-900'>
                  {inspectingProsumer.address || "No installation address registered"}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>Account Registered</p>
                <p className='font-medium text-ink-900'>
                  {formatDate(inspectingProsumer.createdAtUtc)}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>Grid Trading Eligibility</p>
                <p className='font-medium text-ink-900'>
                  {inspectingProsumer.isActive
                    ? "Active (Eligible for Power Trading)"
                    : "Inactive (Approval Pending / Suspended)"}
                </p>
              </div>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}
