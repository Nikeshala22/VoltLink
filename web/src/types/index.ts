export type UserRole = 'Backoffice' | 'GridOperator' | 'Prosumer'


export type ReservationStatus =
  | 'Pending'
  | 'Approved'
  | 'Cancelled'
  | 'Rejected'
  | 'Completed'


export type ReservationType = 'Injection' | 'Withdrawal'


export interface User {
  id: string
  fullName: string
  email: string
  phone: string | null
  address: string | null
  role: UserRole
  isActive: boolean
  deactivationRequested: boolean
  createdAtUtc: string
}


export interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
  user: User
}


export interface OperatingHours {
  openTime: string
  closeTime: string
}


export interface Station {
  id: string
  code: string
  name: string
  addressLine: string
  city: string
  latitude: number
  longitude: number
  capacityKwh: number
  totalBatterySlots: number
  availableBatterySlots: number
  operatingHours: OperatingHours
  isActive: boolean
  createdAtUtc: string
  updatedAtUtc: string
}


export interface NearbyStation {
  station: Station
  distanceMeters: number
}


export interface Slot {
  id: string
  stationId: string
  startTimeUtc: string
  endTimeUtc: string
  capacity: number
  bookedCount: number
  remainingCapacity: number
  energyKwhPerSlot: number
  isActive: boolean
}


export interface Reservation {
  id: string
  reservationNo: string
  prosumerNic: string
  prosumerName: string | null
  stationId: string
  stationName: string | null
  slotId: string
  reservationStartUtc: string
  reservationEndUtc: string
  energyKwh: number
  type: ReservationType
  status: ReservationStatus
  canBeModified: boolean
  canBeCancelled: boolean
  hasQrCode: boolean
  createdAtUtc: string
  cancelledAtUtc: string | null
  completedAtUtc: string | null
}


export interface ReservationSummary {
  action: string
  message: string
  reservation: Reservation
}


export interface ProsumerDashboard {
  prosumerNic: string
  pendingCount: number
  approvedFutureCount: number
  completedCount: number
  cancelledCount: number
  nextReservation: Reservation | null
}


export interface OperatorDashboard {
  pendingCount: number
  approvedFutureCount: number
  completedTodayCount: number
  activeStationCount: number
  todaySchedule: Reservation[]
}
