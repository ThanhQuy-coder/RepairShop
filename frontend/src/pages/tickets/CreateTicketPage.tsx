import { useEffect } from 'react';
import { AnimatePresence, motion } from 'framer-motion';
import { useLocation } from 'react-router-dom';

import { useTicketIntakeStore, type AppointmentIntakeData, type WizardStep } from '../../store/ticketIntakeStore';

import IntakeCustomerStep from '../../components/ticket/IntakeCustomerStep';
import IntakeDeviceStep from '../../components/ticket/IntakeDeviceStep';
import IntakeIssueStep from '../../components/ticket/IntakeIssueStep';
import IntakeImagesStep from '../../components/ticket/IntakeImagesStep';
import IntakeSuccessStep from '../../components/ticket/IntakeSuccessStep';

import styles from './CreateTicketPage.module.css';

const stepVariants = {
  initial: { opacity: 0, x: 24 },
  animate: { opacity: 1, x: 0 },
  exit: { opacity: 0, x: -24 },
};

const STEP_LABELS: { key: WizardStep; label: string }[] = [
  { key: 'customer', label: '1. Khách hàng' },
  { key: 'device', label: '2. Thiết bị' },
  { key: 'issue', label: '3. Tình trạng' },
  { key: 'images', label: '4. Hình ảnh' },
];

export default function CreateTicketPage() {
  const location = useLocation();

  const appointment =
    (
      location.state as {
        appointment?: AppointmentIntakeData;
      } | null
    )?.appointment ?? null;

  const step = useTicketIntakeStore((s) => s.step);
  const customer = useTicketIntakeStore((s) => s.customer);
  const device = useTicketIntakeStore((s) => s.device);
  const createdTicket = useTicketIntakeStore((s) => s.createdTicket);
  const setCreatedTicket = useTicketIntakeStore((s) => s.setCreatedTicket);
  const setStep = useTicketIntakeStore((s) => s.setStep);
  const initializeFromAppointment = useTicketIntakeStore((s) => s.initializeFromAppointment);
  const currentStepIndex = STEP_LABELS.findIndex((s) => s.key === step);

  useEffect(() => {
    initializeFromAppointment(appointment);
  }, [location.key, initializeFromAppointment, appointment?.id]);

  return (
    <div className={styles.wrapper}>
      <div className={styles.topHeader}>
        <span className={styles.eyebrow}>QUY TRÌNH DỊCH VỤ</span>
        <h1 className={styles.title}>Tiếp nhận thiết bị mới</h1>
        <p className={styles.desc}>
          Từng bước ghi nhận thông tin khách hàng, kiểm tra thiết bị và xác nhận tình trạng.
        </p>
      </div>
      {step !== 'success' && (
        <div className={styles.stepper}>
          {STEP_LABELS.map((s, i) => {
            const isCompleted = i < currentStepIndex;
            const isCurrent = i === currentStepIndex;
            return (
              <div
                key={s.key}
                className={`${styles.stepItem} ${isCurrent ? styles.stepCurrent : ''} ${isCompleted ? styles.stepCompleted : ''}`}
              >
                <div className={styles.stepCircle}>{isCompleted ? '✓' : i + 1}</div>
                <span className={styles.stepTitle}>{s.label.split('. ')[1]}</span>
              </div>
            );
          })}
        </div>
      )}

      <div className={styles.stepContent}>
        <AnimatePresence mode="wait" initial={false}>
          <motion.div
            key={step}
            variants={stepVariants}
            initial="initial"
            animate="animate"
            exit="exit"
            transition={{ duration: 0.22, ease: 'easeOut' }}
          >
            {step === 'customer' && <IntakeCustomerStep />}

            {step === 'device' && customer && <IntakeDeviceStep />}

            {step === 'issue' && customer && device && (
              <IntakeIssueStep
                customer={customer}
                device={device}
                onBack={() => setStep('device')}
                onCreated={setCreatedTicket}
              />
            )}

            {step === 'images' && createdTicket && (
              <IntakeImagesStep ticket={createdTicket} onDone={() => setStep('success')} />
            )}

            {step === 'success' && createdTicket && (
              <IntakeSuccessStep ticket={createdTicket} customer={customer} device={device} />
            )}
          </motion.div>
        </AnimatePresence>
      </div>
    </div>
  );
}
