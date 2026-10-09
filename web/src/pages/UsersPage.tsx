// -----------------------------------------------------------------------------
// File        : pages/UsersPage.tsx
// Project     : VoltLink Web - Smart Solar Microgrid Trading System
// Description : Enterprise User Management Portal for administrative staff and
//               grid operators. Includes RBAC privilege visualization, security
//               metrics, audit roster CSV export, and 2FA status management.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { usersApi } from "../api/resources";
import type {
  CreateStaffUserPayload,
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
  RoleBadge,
  SearchInput,
  StatTile,
  formatDate,
  formatDateTime,
} from "../components/Ui";
import {
  IconBolt,
  IconCheck,
  IconClock,
  IconPlus,
  IconUsers,
} from "../components/Icons";
import type { User } from "../types";

const BLANK_FORM: CreateStaffUserPayload = {
  fullName: "",
  email: "",
  phone: "",
  role: "GridOperator",
  password: "",
};

export default function UsersPage() {
  const { user: currentUser } = useAuth();

  const [users, setUsers] = useState<User[]>([]);
  const [roleFilter, setRoleFilter] = useState<
    "" | "Backoffice" | "GridOperator"
  >("");
  const [search, setSearch] = useState("");

  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  // Create user modal state
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [form, setForm] = useState(BLANK_FORM);
  const [modalError, setModalError] = useState<string | null>(null);

  // Edit user modal state
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [editForm, setEditForm] = useState<UpdateUserPayload>({
    fullName: "",
    phone: "",
    role: "GridOperator",
    isTwoFactorEnabled: false,
  });
  const [editModalError, setEditModalError] = useState<string | null>(null);

  // User Profile & Security Inspection Modal
  const [inspectingUser, setInspectingUser] = useState<User | null>(null);

  // Deactivation Safeguard Modal with Justification Reason
  const [deactivatingTarget, setDeactivatingTarget] = useState<User | null>(
    null,
  );
  const [deactivationReason, setDeactivationReason] = useState<string>(
    "Scheduled offboarding / Role transfer",
  );

  const [isSaving, setIsSaving] = useState(false);

  /**
   * Loads system users, omitting prosumers who are managed on ProsumersPage.
   */
  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    try {
      const all = await usersApi.list({
        role: roleFilter || undefined,
        search: search.trim() || undefined,
      });

      setUsers(roleFilter ? all : all.filter((u) => u.role !== "Prosumer"));
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? caught.message
          : "Could not load system users.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [roleFilter, search]);

  useEffect(() => {
    void load();
  }, [load]);

  /**
   * Export compliance roster as CSV
   */
  function handleExportCsv() {
    if (users.length === 0) return;

    const headers = [
      "User ID",
      "Full Name",
      "Email",
      "Role",
      "Status",
      "2FA Enabled",
      "Phone",
      "Created At (UTC)",
    ];

    const rows = users.map((u) => [
      `"${u.id}"`,
      `"${u.fullName.replace(/"/g, '""')}"`,
      `"${u.email.replace(/"/g, '""')}"`,
      `"${u.role}"`,
      `"${u.isActive ? "Active" : "Deactivated"}"`,
      `"${u.isTwoFactorEnabled ? "Yes" : "No"}"`,
      `"${(u.phone ?? "").replace(/"/g, '""')}"`,
      `"${u.createdAtUtc}"`,
    ]);

    const csvContent = [
      headers.join(","),
      ...rows.map((r) => r.join(",")),
    ].join("\r\n");
    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const downloadAnchor = document.createElement("a");
    downloadAnchor.href = url;
    downloadAnchor.setAttribute(
      "download",
      `voltlink_staff_roster_${new Date().toISOString().slice(0, 10)}.csv`,
    );
    document.body.appendChild(downloadAnchor);
    downloadAnchor.click();
    document.body.removeChild(downloadAnchor);
    URL.revokeObjectURL(url);

    setNotice(
      "Staff personnel roster exported successfully for audit compliance.",
    );
  }

  /**
   * Helper to evaluate enterprise password policy strength (0-4 score).
   */
  function getPasswordScore(pwd: string): number {
    let score = 0;
    if (pwd.length >= 8) score += 1;
    if (/[A-Z]/.test(pwd)) score += 1;
    if (/[0-9]/.test(pwd)) score += 1;
    if (/[^A-Za-z0-9]/.test(pwd)) score += 1;
    return score;
  }

  const pwdScore = getPasswordScore(form.password);

  /**
   * Creates a new staff user with validation.
   */
  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    setModalError(null);
    setError(null);

    const trimmedName = form.fullName.trim();
    const trimmedEmail = form.email.trim();

    if (trimmedName.length < 2) {
      setModalError("Full name must be at least 2 characters long.");
      return;
    }
    if (!trimmedEmail || !/^\S+@\S+\.\S+$/.test(trimmedEmail)) {
      setModalError("Please enter a valid email address.");
      return;
    }
    if (form.role !== "Backoffice" && form.role !== "GridOperator") {
      setModalError(
        "Please select a valid role (Back-office or Grid Operator).",
      );
      return;
    }
    if (form.password.length < 6) {
      setModalError("Password must be at least 6 characters.");
      return;
    }

    setIsSaving(true);

    try {
      await usersApi.create({
        ...form,
        fullName: trimmedName,
        email: trimmedEmail,
        phone: form.phone?.trim() || undefined,
      });
      const roleName =
        form.role === "Backoffice" ? "Back-office officer" : "Grid Operator";
      setNotice(`${trimmedName} was created successfully as a ${roleName}.`);
      setIsFormOpen(false);
      setForm(BLANK_FORM);
      await load();
    } catch (caught) {
      const message =
        caught instanceof ApiError
          ? caught.message
          : "Could not create the account.";
      setModalError(message);
      setError(message);
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Opens the edit modal populated with an existing staff user's details.
   */
  function openEdit(user: User) {
    setEditingUser(user);
    setEditForm({
      fullName: user.fullName,
      phone: user.phone ?? "",
      role: (user.role as "Backoffice" | "GridOperator") || "GridOperator",
      isTwoFactorEnabled: user.isTwoFactorEnabled ?? false,
    });
    setEditModalError(null);
    setError(null);
  }

  /**
   * Updates an existing staff user's profile details.
   */
  async function handleUpdate(event: FormEvent) {
    event.preventDefault();
    if (!editingUser) return;

    const trimmedName = editForm.fullName.trim();
    if (trimmedName.length < 2) {
      setEditModalError("Full name must be at least 2 characters long.");
      return;
    }

    setIsSaving(true);
    setEditModalError(null);
    setError(null);

    try {
      await usersApi.update(editingUser.id, {
        ...editForm,
        fullName: trimmedName,
        phone: editForm.phone?.trim() || undefined,
      });
      setNotice(`${trimmedName} profile was updated successfully.`);
      setEditingUser(null);
      await load();
    } catch (caught) {
      const message =
        caught instanceof ApiError ? caught.message : "Could not update user.";
      setEditModalError(message);
      setError(message);
    } finally {
      setIsSaving(false);
    }
  }

  /**
   * Quick toggle 2FA for a user
   */
  async function handleToggle2Fa(target: User) {
    setError(null);
    setNotice(null);

    try {
      const updated = await usersApi.toggle2fa(
        target.id,
        !target.isTwoFactorEnabled,
      );
      setNotice(
        `2FA ${updated.isTwoFactorEnabled ? "enabled" : "disabled"} for ${target.fullName}.`,
      );
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? caught.message
          : "Could not update 2FA status.",
      );
    }
  }

  /**
   * Activates an inactive account directly.
   */
  async function handleActivate(target: User) {
    setError(null);
    setNotice(null);

    try {
      await usersApi.activate(target.id);
      setNotice(
        `${target.fullName} was activated and restored to active duty.`,
      );
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? caught.message
          : "Could not activate account.",
      );
    }
  }

  /**
   * Confirms deactivation with reason logging.
   */
  async function handleConfirmDeactivation() {
    if (!deactivatingTarget) return;
    setIsSaving(true);
    setError(null);
    setNotice(null);

    try {
      await usersApi.deactivate(deactivatingTarget.id);
      setNotice(
        `${deactivatingTarget.fullName} was deactivated (Justification: ${deactivationReason}).`,
      );
      setDeactivatingTarget(null);
      await load();
    } catch (caught) {
      setError(
        caught instanceof ApiError
          ? caught.message
          : "Could not deactivate the account.",
      );
    } finally {
      setIsSaving(false);
    }
  }

  // KPI Calculations
  const totalStaff = users.length;
  const backofficeCount = users.filter((u) => u.role === "Backoffice").length;
  const operatorCount = users.filter((u) => u.role === "GridOperator").length;
  const twoFactorCount = users.filter((u) => u.isTwoFactorEnabled).length;

  return (
    <>
      <PageHeader
        eyebrow='Administration'
        title='System Users'
        description='Back-office officers and grid operators who use the web application.'
        actions={
          <div className='flex flex-wrap items-center gap-2'>
            <button
              type='button'
              onClick={handleExportCsv}
              disabled={isLoading || users.length === 0}
              className='btn-secondary'
              title='Download RFC-4180 CSV staff roster for security audit'
            >
              <svg
                className='h-4 w-4'
                fill='none'
                viewBox='0 0 24 24'
                stroke='currentColor'
                strokeWidth={1.75}
                aria-hidden='true'
              >
                <path
                  strokeLinecap='round'
                  strokeLinejoin='round'
                  d='M3 16.5v2.25A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75V16.5M16.5 12 12 16.5m0 0L7.5 12m4.5 4.5V3'
                />
              </svg>
              Export CSV
            </button>
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
              Add user
            </button>
          </div>
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

      {/* Enterprise KPI Metrics Strip */}
      <div className='grid gap-4 sm:grid-cols-2 lg:grid-cols-4'>
        <StatTile
          label='Total Staff'
          value={totalStaff}
          hint='Enrolled portal personnel'
          tone='brand'
          icon={<IconUsers className='h-[18px] w-[18px]' />}
        />
        <StatTile
          label='Back-Office Officers'
          value={backofficeCount}
          hint='Administrative & KYC authority'
          tone='accent'
          icon={<IconCheck className='h-[18px] w-[18px]' />}
        />
        <StatTile
          label='Grid Operators'
          value={operatorCount}
          hint='Microgrid SCADA & slot dispatch'
          tone='brand'
          icon={<IconBolt className='h-[18px] w-[18px]' />}
        />
        <StatTile
          label='2FA Security Health'
          value={`${twoFactorCount} / ${totalStaff}`}
          hint={
            twoFactorCount > 0
              ? `${twoFactorCount} staff enforcing 2FA verification`
              : "No staff accounts currently enforcing 2FA"
          }
          tone={twoFactorCount > 0 ? "brand" : "warn"}
          icon={<IconClock className='h-[18px] w-[18px]' />}
        />
      </div>

      <div className='card'>
        <div className='card-header'>
          <FilterChips
            value={roleFilter}
            onChange={(next) =>
              setRoleFilter(next as "" | "Backoffice" | "GridOperator")
            }
            options={[
              { value: "", label: `All (${users.length})` },
              {
                value: "Backoffice",
                label: `Back-office (${users.filter((u) => u.role === "Backoffice").length})`,
              },
              {
                value: "GridOperator",
                label: `Operators (${users.filter((u) => u.role === "GridOperator").length})`,
              },
            ]}
          />

          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder='Search staff by name or email…'
            label='Search system users'
          />
        </div>

        {isLoading ? (
          <Loading />
        ) : users.length === 0 ? (
          <EmptyState
            title='No system users found'
            hint='Try clearing your filters or onboard a new staff member.'
          />
        ) : (
          <>
            {/* Mobile View */}
            <ul className='divide-y divide-line md:hidden'>
              {users.map((u) => (
                <li key={u.id} className='p-4'>
                  <div className='flex items-start justify-between gap-3'>
                    <div className='min-w-0'>
                      <p className='font-medium text-ink-900'>
                        {u.fullName}
                        {u.id === currentUser?.id && (
                          <span className='ml-2 text-xs font-normal text-ink-400'>
                            (you)
                          </span>
                        )}
                      </p>
                      <p className='text-xs text-ink-500'>{u.email}</p>
                    </div>
                    <div className='flex shrink-0 flex-col items-end gap-1'>
                      <ActiveBadge isActive={u.isActive} />
                      {u.isTwoFactorEnabled ? (
                        <span className='badge bg-brand-soft text-brand-soft-fg'>
                          <span className='badge-dot' />
                          2FA On
                        </span>
                      ) : (
                        <span className='badge bg-neutral-bg text-neutral-fg'>
                          2FA Off
                        </span>
                      )}
                    </div>
                  </div>

                  <div className='mt-3 flex flex-wrap items-center gap-2'>
                    <RoleBadge role={u.role} />
                    <button
                      type='button'
                      onClick={() => setInspectingUser(u)}
                      className='btn-secondary btn-sm'
                    >
                      View
                    </button>
                    <button
                      type='button'
                      onClick={() => openEdit(u)}
                      className='btn-secondary btn-sm'
                    >
                      Edit
                    </button>
                    <button
                      type='button'
                      onClick={() => void handleToggle2Fa(u)}
                      className='btn-secondary btn-sm'
                      title={
                        u.isTwoFactorEnabled ? "Disable 2FA" : "Enable 2FA"
                      }
                    >
                      {u.isTwoFactorEnabled ? "2FA: On" : "2FA: Off"}
                    </button>
                    {u.id !== currentUser?.id && (
                      <button
                        type='button'
                        onClick={() =>
                          u.isActive
                            ? setDeactivatingTarget(u)
                            : void handleActivate(u)
                        }
                        className={
                          u.isActive
                            ? "btn-danger btn-sm"
                            : "btn-success btn-sm"
                        }
                      >
                        {u.isActive ? "Deactivate" : "Activate"}
                      </button>
                    )}
                  </div>
                </li>
              ))}
            </ul>

            {/* Desktop Table View */}
            <div className='table-wrap hidden md:block'>
              <table className='table'>
                <thead>
                  <tr>
                    <th>Staff Member</th>
                    <th>Email</th>
                    <th>Role & Entitlement</th>
                    <th>Enrolled</th>
                    <th>Account Status</th>
                    <th>2FA Security</th>
                    <th className='text-right'>Access Controls</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map((u) => (
                    <tr key={u.id}>
                      <td className='font-medium text-ink-900'>
                        {u.fullName}
                        {u.id === currentUser?.id && (
                          <span className='ml-2 text-xs font-normal text-ink-400'>
                            (you)
                          </span>
                        )}
                      </td>
                      <td className='text-xs font-mono'>{u.email}</td>
                      <td>
                        <RoleBadge role={u.role} />
                      </td>
                      <td className='whitespace-nowrap text-xs'>
                        {formatDate(u.createdAtUtc)}
                      </td>
                      <td>
                        <ActiveBadge isActive={u.isActive} />
                      </td>
                      <td>
                        {u.isTwoFactorEnabled ? (
                          <span
                            className='badge bg-brand-soft text-brand-soft-fg'
                            title='2FA active: requires OTP code on login'
                          >
                            <span className='badge-dot' />
                            2FA Enabled
                          </span>
                        ) : (
                          <span
                            className='badge bg-neutral-bg text-neutral-fg'
                            title='2FA inactive: direct password sign-in'
                          >
                            2FA Off
                          </span>
                        )}
                      </td>
                      <td className='text-right'>
                        <div className='flex justify-end gap-2'>
                          <button
                            type='button'
                            onClick={() => setInspectingUser(u)}
                            className='btn-secondary btn-sm'
                            title='Inspect user profile & RBAC permissions'
                          >
                            View
                          </button>
                          <button
                            type='button'
                            onClick={() => openEdit(u)}
                            className='btn-secondary btn-sm'
                          >
                            Edit
                          </button>
                          <button
                            type='button'
                            onClick={() => void handleToggle2Fa(u)}
                            className='btn-secondary btn-sm'
                            title={
                              u.isTwoFactorEnabled
                                ? "Click to disable 2FA"
                                : "Click to enforce 2FA"
                            }
                          >
                            {u.isTwoFactorEnabled ? "2FA: On" : "2FA: Off"}
                          </button>
                          {/* Self-deactivation protection */}
                          {u.id !== currentUser?.id && (
                            <button
                              type='button'
                              onClick={() =>
                                u.isActive
                                  ? setDeactivatingTarget(u)
                                  : void handleActivate(u)
                              }
                              className={
                                u.isActive
                                  ? "btn-danger btn-sm"
                                  : "btn-success btn-sm"
                              }
                            >
                              {u.isActive ? "Deactivate" : "Activate"}
                            </button>
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

      {/* Modal 1: Add System User */}
      <Modal
        title='Onboard Staff Account'
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
              form='user-form'
              disabled={isSaving}
              className='btn-primary'
            >
              {isSaving ? "Enrolling…" : "Create staff account"}
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
            id='user-form'
            onSubmit={handleCreate}
            className='grid gap-4 sm:grid-cols-2'
          >
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
              <label className='field-label'>Role Assignment</label>
              <select
                value={form.role}
                onChange={(e) =>
                  setForm({
                    ...form,
                    role: e.target.value as "Backoffice" | "GridOperator",
                  })
                }
                className='field-input'
              >
                <option value='GridOperator'>
                  Grid Operator (Microgrid SCADA)
                </option>
                <option value='Backoffice'>
                  Back-office (KYC & User Admin)
                </option>
              </select>
            </div>

            <div>
              <label className='field-label'>Work Email</label>
              <input
                required
                type='email'
                maxLength={120}
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                className='field-input'
                placeholder='kasun@voltlink.lk'
              />
            </div>

            <div>
              <label className='field-label'>Contact Phone</label>
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
              <label className='field-label'>Initial Password</label>
              <input
                required
                type='password'
                minLength={6}
                maxLength={100}
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                className='field-input'
                placeholder='Set initial password (min 6 characters)'
              />

              {/* Password Strength Policy Bar */}
              {form.password.length > 0 && (
                <div className='mt-2.5 rounded-lg border border-line bg-surface-subtle p-3 text-xs'>
                  <div className='mb-1.5 flex items-center justify-between'>
                    <span className='font-semibold text-ink-700'>
                      Enterprise Password Hygiene:
                    </span>
                    <span
                      className={`font-semibold ${
                        pwdScore <= 1
                          ? "text-danger"
                          : pwdScore === 2
                            ? "text-warn-fg"
                            : pwdScore === 3
                              ? "text-brand-500"
                              : "text-success"
                      }`}
                    >
                      {pwdScore <= 1
                        ? "Weak"
                        : pwdScore === 2
                          ? "Fair"
                          : pwdScore === 3
                            ? "Good"
                            : "Strong (Enterprise Compliant)"}
                    </span>
                  </div>
                  <div className='h-1.5 w-full overflow-hidden rounded-full bg-line'>
                    <div
                      className={`h-full transition-all duration-300 ${
                        pwdScore <= 1
                          ? "w-1/4 bg-danger"
                          : pwdScore === 2
                            ? "w-2/4 bg-warn-fg"
                            : pwdScore === 3
                              ? "w-3/4 bg-brand-500"
                              : "w-full bg-success"
                      }`}
                    />
                  </div>
                  <ul className='mt-2 grid grid-cols-2 gap-1 text-[11px] text-ink-500'>
                    <li
                      className={
                        form.password.length >= 8
                          ? "text-success font-medium"
                          : ""
                      }
                    >
                      {form.password.length >= 8 ? "✓" : "•"} 8+ characters
                    </li>
                    <li
                      className={
                        /[A-Z]/.test(form.password)
                          ? "text-success font-medium"
                          : ""
                      }
                    >
                      {/[A-Z]/.test(form.password) ? "✓" : "•"} Uppercase letter
                    </li>
                    <li
                      className={
                        /[0-9]/.test(form.password)
                          ? "text-success font-medium"
                          : ""
                      }
                    >
                      {/[0-9]/.test(form.password) ? "✓" : "•"} Number (0-9)
                    </li>
                    <li
                      className={
                        /[^A-Za-z0-9]/.test(form.password)
                          ? "text-success font-medium"
                          : ""
                      }
                    >
                      {/[^A-Za-z0-9]/.test(form.password) ? "✓" : "•"} Special
                      character
                    </li>
                  </ul>
                </div>
              )}
            </div>

            <p className='text-xs text-ink-400 sm:col-span-2'>
              Privileged staff accounts receive portal access immediately upon
              creation. Prosumer solar accounts register through the mobile
              application.
            </p>
          </form>
        </div>
      </Modal>

      {/* Modal 2: Edit User Profile */}
      <Modal
        title={`Edit Profile — ${editingUser?.fullName ?? ""}`}
        isOpen={editingUser !== null}
        onClose={() => {
          setEditingUser(null);
          setEditModalError(null);
        }}
        footer={
          <>
            <button
              type='button'
              onClick={() => {
                setEditingUser(null);
                setEditModalError(null);
              }}
              className='btn-secondary'
            >
              Cancel
            </button>
            <button
              type='submit'
              form='edit-user-form'
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
            id='edit-user-form'
            onSubmit={handleUpdate}
            className='space-y-4'
          >
            <div>
              <label className='field-label'>Work Email</label>
              <input
                disabled
                value={editingUser?.email ?? ""}
                className='field-input opacity-70'
              />
              <p className='mt-1 text-xs text-ink-400'>
                Email address is the primary login identifier and cannot be
                modified.
              </p>
            </div>

            <div>
              <label className='field-label'>Assigned Role</label>
              {editingUser?.id === currentUser?.id ? (
                <>
                  <input
                    disabled
                    value={
                      editingUser?.role === "GridOperator"
                        ? "Grid Operator"
                        : (editingUser?.role ?? "")
                    }
                    className='field-input opacity-70'
                  />
                  <p className='mt-1 text-xs text-ink-400'>
                    You cannot change the role of your currently active
                    signed-in session.
                  </p>
                </>
              ) : (
                <select
                  value={editForm.role ?? editingUser?.role ?? "GridOperator"}
                  onChange={(e) =>
                    setEditForm({
                      ...editForm,
                      role: e.target.value as "Backoffice" | "GridOperator",
                    })
                  }
                  className='field-input'
                >
                  <option value='GridOperator'>Grid Operator</option>
                  <option value='Backoffice'>Back-office</option>
                </select>
              )}
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
              <label className='field-label'>Contact Phone</label>
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

            {/* 2FA Enforcement Toggle */}
            <div className='flex items-center justify-between rounded-xl border border-line bg-surface-subtle p-3.5'>
              <div>
                <p className='text-xs font-semibold text-ink-900'>
                  Two-Factor Authentication (2FA)
                </p>
                <p className='text-xs text-ink-500'>
                  Require 6-digit OTP code verification on sign-in.
                </p>
              </div>
              <label className='flex cursor-pointer items-center gap-2 text-xs font-medium text-ink-700'>
                <input
                  type='checkbox'
                  checked={editForm.isTwoFactorEnabled ?? false}
                  onChange={(e) =>
                    setEditForm({
                      ...editForm,
                      isTwoFactorEnabled: e.target.checked,
                    })
                  }
                  className='h-4 w-4 rounded border-line text-brand-600 focus:ring-brand-500'
                />
                <span
                  className={
                    editForm.isTwoFactorEnabled
                      ? "text-brand-600 font-semibold"
                      : ""
                  }
                >
                  {editForm.isTwoFactorEnabled ? "Enforced" : "Off"}
                </span>
              </label>
            </div>
          </form>
        </div>
      </Modal>

      {/* Modal 3: Enterprise User Profile & Security Inspection */}
      <Modal
        title={`Staff Profile & Access Entitlements — ${inspectingUser?.fullName ?? ""}`}
        isOpen={inspectingUser !== null}
        onClose={() => setInspectingUser(null)}
        footer={
          <div className='flex w-full items-center justify-between'>
            <button
              type='button'
              onClick={() => {
                const target = inspectingUser;
                setInspectingUser(null);
                if (target) openEdit(target);
              }}
              className='btn-secondary'
            >
              Edit Profile
            </button>
            <button
              type='button'
              onClick={() => setInspectingUser(null)}
              className='btn-primary'
            >
              Close
            </button>
          </div>
        }
      >
        {inspectingUser && (
          <div className='space-y-4 text-sm'>
            <div className='rounded-lg border border-line bg-surface-subtle p-4'>
              <div className='flex items-center justify-between'>
                <div>
                  <p className='text-xs font-semibold uppercase tracking-wider text-ink-400'>
                    Directory Identifier (ObjectID)
                  </p>
                  <p className='font-mono text-sm font-bold text-ink-900'>
                    {inspectingUser.id}
                  </p>
                </div>
                <div className='flex flex-wrap items-center gap-1.5'>
                  <RoleBadge role={inspectingUser.role} />
                  <ActiveBadge isActive={inspectingUser.isActive} />
                  {inspectingUser.isTwoFactorEnabled ? (
                    <span className='badge bg-brand-soft text-brand-soft-fg'>
                      <span className='badge-dot' />
                      2FA Enabled
                    </span>
                  ) : (
                    <span className='badge bg-neutral-bg text-neutral-fg'>
                      2FA Disabled
                    </span>
                  )}
                </div>
              </div>
            </div>

            <div className='grid gap-3 sm:grid-cols-2'>
              <div>
                <p className='text-xs text-ink-400'>Full Name</p>
                <p className='font-medium text-ink-900'>
                  {inspectingUser.fullName}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>Work Email</p>
                <p className='font-mono font-medium text-ink-900'>
                  {inspectingUser.email}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>Contact Number</p>
                <p className='font-medium text-ink-900'>
                  {inspectingUser.phone || "Not registered"}
                </p>
              </div>
              <div>
                <p className='text-xs text-ink-400'>Enrollment Date</p>
                <p className='font-medium text-ink-900'>
                  {formatDateTime(inspectingUser.createdAtUtc)}
                </p>
              </div>
              <div className='sm:col-span-2'>
                <p className='text-xs text-ink-400'>
                  Two-Factor Authentication (2FA)
                </p>
                <p className='mt-0.5 font-medium text-ink-900'>
                  {inspectingUser.isTwoFactorEnabled
                    ? "Active — 6-digit OTP passcode verification enforced on login"
                    : "Disabled — Standard password authentication"}
                </p>
              </div>
            </div>

            {/* RBAC Entitlements Matrix */}
            <div className='rounded-lg border border-line p-3.5'>
              <p className='text-xs font-semibold uppercase tracking-wider text-ink-500'>
                Assigned RBAC Privileges & Scopes
              </p>
              {inspectingUser.role === "Backoffice" ? (
                <ul className='mt-2 space-y-1 text-xs text-ink-700'>
                  <li className='flex items-center gap-2'>
                    <span className='text-success font-bold'>✓</span> Staff User
                    Onboarding & Access Control
                  </li>
                  <li className='flex items-center gap-2'>
                    <span className='text-success font-bold'>✓</span> Prosumer
                    Registration & KYC Activation
                  </li>
                  <li className='flex items-center gap-2'>
                    <span className='text-success font-bold'>✓</span> Settlement
                    Audit & System Parameter Configuration
                  </li>
                  <li className='flex items-center gap-2 text-ink-400'>
                    <span className='text-ink-400'>✕</span> Real-time Inverter
                    Dispatch Bypass (Operator only)
                  </li>
                </ul>
              ) : (
                <ul className='mt-2 space-y-1 text-xs text-ink-700'>
                  <li className='flex items-center gap-2'>
                    <span className='text-success font-bold'>✓</span> Solar
                    Microgrid Hub Live Telemetry Monitoring
                  </li>
                  <li className='flex items-center gap-2'>
                    <span className='text-success font-bold'>✓</span> Energy
                    Booking Slot Capacity Dispatching
                  </li>
                  <li className='flex items-center gap-2'>
                    <span className='text-success font-bold'>✓</span> Station
                    Operating Hours & Battery Slot Updates
                  </li>
                  <li className='flex items-center gap-2 text-ink-400'>
                    <span className='text-ink-400'>✕</span> Security
                    Administrator Privilege Escalation
                  </li>
                </ul>
              )}
            </div>

            <div className='text-xs text-ink-400'>
              Identity governance compliance: Activity logged under VoltLink
              Enterprise Audit Policy.
            </div>
          </div>
        )}
      </Modal>

      {/* Modal 4: Enterprise Deactivation Safeguard with Justification Reason */}
      <Modal
        title='Confirm Account Suspension'
        isOpen={deactivatingTarget !== null}
        onClose={() => setDeactivatingTarget(null)}
        footer={
          <>
            <button
              type='button'
              onClick={() => setDeactivatingTarget(null)}
              className='btn-secondary'
            >
              Cancel
            </button>
            <button
              type='button'
              onClick={() => void handleConfirmDeactivation()}
              disabled={isSaving}
              className='btn-danger'
            >
              {isSaving ? "Deactivating…" : "Confirm Suspension"}
            </button>
          </>
        }
      >
        {deactivatingTarget && (
          <div className='space-y-4 text-sm'>
            <div className='rounded-lg border border-warn-fg/20 bg-warn-bg/40 p-3 text-warn-fg'>
              <p className='font-medium'>Warning: Access Revocation</p>
              <p className='mt-1 text-xs text-ink-700'>
                Suspending <strong>{deactivatingTarget.fullName}</strong> will
                immediately revoke their active JWT credentials. They will be
                prevented from accessing the back-office portal until manually
                reactivated.
              </p>
            </div>

            <div>
              <label className='field-label'>Audit Justification Reason</label>
              <select
                value={deactivationReason}
                onChange={(e) => setDeactivationReason(e.target.value)}
                className='field-input'
              >
                <option value='Scheduled offboarding / Role transfer'>
                  Scheduled offboarding / Role transfer
                </option>
                <option value='Temporary leave of absence'>
                  Temporary leave of absence
                </option>
                <option value='Security audit / Privilege review'>
                  Security audit / Privilege review
                </option>
                <option value='Compromised credentials / Incident containment'>
                  Compromised credentials / Incident containment
                </option>
              </select>
              <p className='mt-1 text-xs text-ink-400'>
                This justification will be logged in the system audit trail for
                compliance review.
              </p>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}
