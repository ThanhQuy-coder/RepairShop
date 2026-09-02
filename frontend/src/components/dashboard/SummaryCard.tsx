import styles from './SummaryCard.module.css';

interface SummaryCardProps {
  icon: string;
  label: string;
  value: number;
  isEmphasized?: boolean; // dùng cho ô "Total" — nổi bật hơn các ô còn lại
}

export default function SummaryCard({ icon, label, value, isEmphasized }: SummaryCardProps) {
  return (
    <div className={`${styles.card} ${isEmphasized ? styles.emphasized : ''}`}>
      <span className={styles.icon}>{icon}</span>
      <div>
        <div className={styles.value}>{value}</div>
        <div className={styles.label}>{label}</div>
      </div>
    </div>
  );
}
