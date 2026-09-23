import { api, buildQuery } from './client'
import type {
  LoginResponse,
  NearbyStation,
  OperatorDashboard,
  Reservation,
  ReservationSummary,
  Slot,
  Station,
  User,
} from '../types'


export const authApi = {
  
  login: (email: string, password: string) =>
    api.post<LoginResponse>('/auth/login', { email, password }),

  
  me: () => api.get<User>('/auth/me'),
}


export interface CreateStaffUserPayload {
  fullName: string
  email: string
  phone?: string
  role: 'Backoffice' | 'GridOperator'
  password: string
}

export interface UpdateUserPayload {
  fullName: string
  phone?: string
  address?: string
}

export const usersApi = {
  list: (filters: { role?: string; isActive?: boolean; search?: string } = {}) =>
    api.get<User[]>(`/users${buildQuery(filters)}`),

  get: (id: string) => api.get<User>(`/users/${id}`),

  create: (payload: CreateStaffUserPayload) => api.post<User>('/users', payload),

  update: (id: string, payload: UpdateUserPayload) => api.put<User>(`/users/${id}`, payload),

  activate: (id: string) => api.patch<User>(`/users/${id}/activate`),

  deactivate: (id: string) => api.patch<User>(`/users/${id}/deactivate`),
}


export interface CreateProsumerPayload {
  nic: string
  fullName: string
  email: string
  phone?: string
  address?: string
  password: string
  activateImmediately: boolean
}

export const prosumersApi = {
  list: (filters: { isActive?: boolean; search?: string } = {}) =>
    api.get<User[]>(`/prosumers${buildQuery(filters)}`),

  
  pending: () => api.get<User[]>('/prosumers/pending'),


  deactivationRequests: () => api.get<User[]>('/prosumers/deactivation-requests'),

  get: (nic: string) => api.get<User>(`/prosumers/${nic}`),

  create: (payload: CreateProsumerPayload) => api.post<User>('/prosumers', payload),

  update: (nic: string, payload: UpdateUserPayload) =>
    api.put<User>(`/prosumers/${nic}`, payload),

  activate: (nic: string) => api.patch<User>(`/prosumers/${nic}/activate`),

  deactivate: (nic: string) => api.patch<User>(`/prosumers/${nic}/deactivate`),
}


export interface StationPayload {
  code?: string
  name: string
  addressLine: string
  city: string
  latitude: number
  longitude: number
  capacityKwh: number
  totalBatterySlots: number
  availableBatterySlots?: number
  openTime: string
  closeTime: string
}

export interface SlotPayload {
  startTimeUtc: string
  endTimeUtc: string
  capacity: number
  energyKwhPerSlot: number
  isActive?: boolean
}

export const stationsApi = {
  list: (filters: { isActive?: boolean; city?: string; search?: string } = {}) =>
    api.get<Station[]>(`/stations${buildQuery(filters)}`),

  get: (id: string) => api.get<Station>(`/stations/${id}`),

  
  nearby: (lat: number, lng: number, radiusKm = 10, limit = 50) =>
    api.get<NearbyStation[]>(`/stations/nearby${buildQuery({ lat, lng, radiusKm, limit })}`),

  create: (payload: StationPayload) => api.post<Station>('/stations', payload),

  update: (id: string, payload: StationPayload) => api.put<Station>(`/stations/${id}`, payload),

  activate: (id: string) => api.patch<Station>(`/stations/${id}/activate`),

  
  deactivate: (id: string) => api.patch<Station>(`/stations/${id}/deactivate`),

  updateBatterySlots: (id: string, availableBatterySlots: number) =>
    api.patch<Station>(`/stations/${id}/battery-slots`, { availableBatterySlots }),

  listSlots: (id: string, filters: { from?: string; to?: string; isActive?: boolean } = {}) =>
    api.get<Slot[]>(`/stations/${id}/slots${buildQuery(filters)}`),

  createSlot: (id: string, payload: SlotPayload) =>
    api.post<Slot>(`/stations/${id}/slots`, payload),
}

export const slotsApi = {
  get: (id: string) => api.get<Slot>(`/slots/${id}`),

  update: (id: string, payload: SlotPayload) => api.put<Slot>(`/slots/${id}`, payload),

  
  remove: (id: string) => api.del<void>(`/slots/${id}`),
}

export interface ReservationFilters {
  nic?: string
  stationId?: string
  status?: string
  from?: string
  to?: string
  q?: string
  limit?: number
}

export const reservationsApi = {
  search: (filters: ReservationFilters = {}) =>
    api.get<Reservation[]>(`/reservations${buildQuery({ ...filters })}`),

  pending: () => api.get<Reservation[]>('/reservations/pending'),

  get: (id: string) => api.get<Reservation>(`/reservations/${id}`),

  create: (slotId: string, type: string, prosumerNic?: string) =>
    api.post<ReservationSummary>('/reservations', { slotId, type, prosumerNic }),

  update: (id: string, slotId: string, type: string) =>
    api.put<ReservationSummary>(`/reservations/${id}`, { slotId, type }),

  cancel: (id: string) => api.patch<ReservationSummary>(`/reservations/${id}/cancel`),

  approve: (id: string) => api.patch<Reservation>(`/reservations/${id}/approve`),

  reject: (id: string) => api.patch<Reservation>(`/reservations/${id}/reject`),

 
  verifyQr: (token: string) => api.post<Reservation>('/reservations/verify-qr', { token }),

  complete: (id: string) => api.post<ReservationSummary>(`/reservations/${id}/complete`),
}


export const dashboardApi = {
 
  operator: () => api.get<OperatorDashboard>('/dashboard/operator'),
}
