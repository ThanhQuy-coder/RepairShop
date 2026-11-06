import { useEffect, useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import type { NavGroup, NavItem } from '../constants/navigation';
import {
  ChevronDown,
  ChevronsUpDown,
  PanelLeftClose,
  PanelLeftOpen,
} from './common/FixoraIcons';
import styles from './Sidebar.module.css';

interface SidebarProps {
  items?: NavItem[];
  groups?: NavGroup[];
  title: string;
}

export default function Sidebar({ items = [], groups = [], title }: SidebarProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const location = useLocation();

  const hasGroups = groups.length > 0;
  const activeGroup = groups.find((group) =>
    group.items.some(
      (item) =>
        location.pathname === item.path || location.pathname.startsWith(`${item.path}/`)
    )
  )?.key;

  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {};
    for (const g of groups) {
      initial[g.key] = g.key === activeGroup;
    }
    if (!activeGroup && groups.length > 0) {
      initial[groups[0].key] = true;
    }
    return initial;
  });

  useEffect(() => {
    if (activeGroup) {
      setExpandedGroups((prev) =>
        prev[activeGroup] ? prev : { ...prev, [activeGroup]: true }
      );
    }
  }, [activeGroup]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, []);

  const closeMenu = () => setIsOpen(false);

  const toggleGroup = (key: string) => {
    if (isCollapsed) {
      setIsCollapsed(false);
      setExpandedGroups((prev) => ({ ...prev, [key]: true }));
      return;
    }
    setExpandedGroups((prev) => ({
      ...prev,
      [key]: !prev[key],
    }));
  };

  const allExpanded =
    hasGroups && groups.every((group) => Boolean(expandedGroups[group.key]));

  const handleToggleAllGroups = () => {
    const nextState = !allExpanded;
    const nextMap: Record<string, boolean> = {};
    for (const g of groups) {
      nextMap[g.key] = nextState || g.key === activeGroup;
    }
    setExpandedGroups(nextMap);
  };

  const renderItem = (item: NavItem) => {
    const shortLabel = item.label.charAt(0).toUpperCase();
    return (
      <NavLink
        key={item.path}
        to={item.path}
        end={item.end}
        onClick={closeMenu}
        title={isCollapsed ? item.label : undefined}
        className={({ isActive }) =>
          `${styles.navItem} ${isActive ? styles.active : ''} ${
            isCollapsed ? styles.navItemCollapsed : ''
          }`
        }
      >
        <span className={styles.itemDot} aria-hidden="true">
          {isCollapsed ? shortLabel : ''}
        </span>
        {!isCollapsed && <span className={styles.itemText}>{item.label}</span>}
      </NavLink>
    );
  };

  return (
    <>
      {/* Hamburger button — chỉ hiện trên màn hình nhỏ */}
      <button
        className={styles.hamburger}
        onClick={() => setIsOpen(true)}
        aria-label="Mở menu"
      >
        ☰
      </button>

      {/* Overlay tối phía sau khi mở sidebar trên mobile */}
      {isOpen && <div className={styles.overlay} onClick={() => setIsOpen(false)} />}

      <aside
        className={`${styles.sidebar} ${isOpen ? styles.sidebarOpen : ''} ${
          isCollapsed ? styles.sidebarCollapsed : ''
        }`}
      >
        <div className={styles.brandRow}>
          {!isCollapsed && <div className={styles.brand}>{title}</div>}

          <div className={styles.brandActions}>
            <button
              type="button"
              className={styles.collapseToggleBtn}
              onClick={() => setIsCollapsed((prev) => !prev)}
              aria-label={isCollapsed ? 'Mở rộng thanh điều hướng' : 'Thu gọn thanh điều hướng'}
              title={isCollapsed ? 'Mở rộng Sidebar' : 'Thu gọn Sidebar'}
            >
              {isCollapsed ? <PanelLeftOpen size={17} /> : <PanelLeftClose size={17} />}
            </button>

            <button
              type="button"
              className={styles.closeButton}
              onClick={() => setIsOpen(false)}
              aria-label="Đóng menu"
            >
              ×
            </button>
          </div>
        </div>

        {hasGroups && !isCollapsed && (
          <div className={styles.groupToolbar}>
            <span className={styles.groupToolbarLabel}>DANH MỤC ĐIỀU HƯỚNG</span>
            <button
              type="button"
              className={styles.expandAllBtn}
              onClick={handleToggleAllGroups}
              title={allExpanded ? 'Thu gọn các nhóm' : 'Mở rộng tất cả nhóm'}
            >
              <ChevronsUpDown size={13} />
              <span>{allExpanded ? 'Thu gọn' : 'Mở tất cả'}</span>
            </button>
          </div>
        )}

        <nav className={styles.nav}>
          {hasGroups
            ? groups.map((group) => {
                const isActive = activeGroup === group.key;
                const isExpanded = Boolean(expandedGroups[group.key]);
                return (
                  <div
                    className={`${styles.group} ${isExpanded ? styles.groupExpanded : ''}`}
                    key={group.key}
                  >
                    <button
                      type="button"
                      className={`${styles.groupButton} ${
                        isActive ? styles.groupActive : ''
                      } ${isCollapsed ? styles.groupButtonCollapsed : ''}`}
                      aria-expanded={isExpanded}
                      aria-controls={`submenu-${group.key}`}
                      onClick={() => toggleGroup(group.key)}
                      title={isCollapsed ? `${group.label} (${group.items.length})` : undefined}
                    >
                      {isCollapsed ? (
                        <span className={styles.collapsedGroupInitial}>
                          {group.label.charAt(0).toUpperCase()}
                        </span>
                      ) : (
                        <>
                          <span className={styles.groupLabelWrap}>
                            <span className={styles.groupTitleText}>{group.label}</span>
                            <span className={styles.groupCountBadge}>
                              {group.items.length}
                            </span>
                          </span>
                          <span
                            className={`${styles.chevronBadge} ${
                              isExpanded ? styles.chevronOpen : ''
                            }`}
                            aria-hidden="true"
                          >
                            <ChevronDown size={14} />
                          </span>
                        </>
                      )}
                    </button>

                    {!isCollapsed && (
                      <div
                        id={`submenu-${group.key}`}
                        className={`${styles.submenuWrapper} ${
                          isExpanded ? styles.submenuWrapperOpen : ''
                        }`}
                      >
                        <div className={styles.submenu}>
                          {group.items.map(renderItem)}
                        </div>
                      </div>
                    )}
                  </div>
                );
              })
            : items.map(renderItem)}
        </nav>
      </aside>
    </>
  );
}
