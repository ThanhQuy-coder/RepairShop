import { useState } from 'react';
import type { Customer } from '../../types/customer.types';
import type { Device } from '../../types/device.types';
import type { Ticket } from '../../types/ticket.types';
import IntakeCustomerStep from '../../components/ticket/IntakeCustomerStep';
import IntakeDeviceStep from '../../components/ticket/IntakeDeviceStep';
import IntakeIssueStep from '../../components/ticket/IntakeIssueStep';
import IntakeImagesStep from '../../components/ticket/IntakeImagesStep';
import IntakeSuccessStep from '../../components/ticket/IntakeSuccessStep';
import styles from './CreateTicketPage.module.css';

type WizardStep = 'customer' | 'device' | 'issue' | 'images' | 'success';

const STEP_LABELS: { key: WizardStep; label: string }[] = [
  { key: 'customer', label: '1. Khách hàng' },
  { key: 'device', label: '2. Thiết bị' },
  { key: 'issue', label: '3. Tình trạng' },
  { key: 'images', label: '4. Hình ảnh' },
];

export default function CreateTicketPage() {
  const [step, setStep] = useState<WizardStep>('customer');
  const [customer, setCustomer] = useState<Customer | null>(null);
  const [device, setDevice] = useState<Device | null>(null);
  const [createdTicket, setCreatedTicket] = useState<Ticket | null>(null);

  const currentStepIndex = STEP_LABELS.findIndex((s) => s.key === step);

  return (
    <div className={styles.wrapper}>
      <h2>Tiếp nhận thiết bị</h2>

      {step !== 'success' && (
        <div className={styles.stepper}>
          {STEP_LABELS.map((s, i) => (
            <span
              key={s.key}
              className={`${styles.stepItem} ${i <= currentStepIndex ? styles.stepActive : ''}`}
            >
              {s.label}
            </span>
          ))}
        </div>
      )}

      <div className={styles.stepContent}>
        {step === 'customer' && (
          <IntakeCustomerStep
            onNext={(c) => {
              setCustomer(c);
              setStep('device');
            }}
          />
        )}

        {step === 'device' && customer && (
          <IntakeDeviceStep
            customer={customer}
            onBack={() => setStep('customer')}
            onNext={(d) => {
              setDevice(d);
              setStep('issue');
            }}
          />
        )}

        {step === 'issue' && customer && device && (
          <IntakeIssueStep
            customer={customer}
            device={device}
            onBack={() => setStep('device')}
            onCreated={(ticket) => {
              setCreatedTicket(ticket);
              setStep('images');
            }}
          />
        )}

        {step === 'images' && createdTicket && (
          <IntakeImagesStep ticket={createdTicket} onDone={() => setStep('success')} />
        )}

        {step === 'success' && createdTicket && (
          <IntakeSuccessStep ticket={createdTicket} customer={customer} device={device} />
        )}
      </div>
    </div>
  );
}
