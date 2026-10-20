import { create } from 'zustand';

import type { Customer } from '../types/customer.types';
import type { Device } from '../types/device.types';
import type { Ticket } from '../types/ticket.types';

export interface AppointmentIntakeData {
  id: string;
  customerId: string;
  fullName: string;
  phone: string;
  appointmentDate: string;
  timeSlotId: string;
  slotStart: string;
  slotEnd: string;
  status: string;
  createdAt: string;
  lookupToken?: string;

  deviceType?: 'Phone' | 'Laptop' | 'Electronics' | 'Tablet' | 'Smartwatch';
  brand?: string;
  model?: string;
  serialNumber?: string;
}

export type WizardStep = 'customer' | 'device' | 'issue' | 'images' | 'success';

interface TicketIntakeState {
  appointmentData: AppointmentIntakeData | null;
  customer: Customer | null;
  device: Device | null;
  createdTicket: Ticket | null;
  step: WizardStep;

  initializeFromAppointment: (appointment: AppointmentIntakeData | null) => void;

  setCustomer: (customer: Customer) => void;
  setDevice: (device: Device) => void;
  setCreatedTicket: (ticket: Ticket) => void;
  setStep: (step: WizardStep) => void;
  reset: () => void;
}

const initialState = {
  appointmentData: null,
  customer: null,
  device: null,
  createdTicket: null,
  step: 'customer' as WizardStep,
};

export const useTicketIntakeStore = create<TicketIntakeState>()((set) => ({
  ...initialState,

  initializeFromAppointment: (appointment) =>
    set({
      ...initialState,
      appointmentData: appointment,
    }),

  setCustomer: (customer) =>
    set({
      customer,
      device: null,
      createdTicket: null,
      step: 'device',
    }),

  setDevice: (device) =>
    set({
      device,
      createdTicket: null,
      step: 'issue',
    }),

  setCreatedTicket: (ticket) =>
    set({
      createdTicket: ticket,
      step: 'images',
    }),

  setStep: (step) => set({ step }),

  reset: () => set(initialState),
}));
