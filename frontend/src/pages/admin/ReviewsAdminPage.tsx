import { useCallback, useEffect, useMemo, useState } from 'react';

import {
  Badge,
  Button,
  ConfirmDialog,
  ErrorMessage,
  Pagination,
  Select,
  Table,
} from '../../components/common';

import type { TableColumn } from '../../components/common/Table';

import { reviewService } from '../../services/reviewService';
import type { ReviewListItem } from '../../types/review.types';

import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

import styles from './ReviewsAdminPage.module.css';

const PAGE_SIZE = 6;

const RATING_OPTIONS = [
  { value: '5', label: '5 sao (★★★★★)' },
  { value: '4', label: '4 sao (★★★★☆)' },
  { value: '3', label: '3 sao (★★★☆☆)' },
  { value: '2', label: '2 sao (★★☆☆☆)' },
  { value: '1', label: '1 sao (★☆☆☆☆)' },
];

export default function ReviewsAdminPage() {
  const { showSuccess } = useToast();

  const [reviews, setReviews] = useState<ReviewListItem[]>([]);
  const [ratingFilter, setRatingFilter] = useState('all');
  const [visibilityFilter, setVisibilityFilter] = useState<'all' | 'visible' | 'hidden'>('all');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [confirmHideId, setConfirmHideId] = useState<string | null>(null);

  const [page, setPage] = useState(1);

  const fetchReviews = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await reviewService.getAdminReviews(1, 200);
      setReviews(response.items);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchReviews();
  }, [fetchReviews]);

  useEffect(() => {
    setPage(1);
  }, [ratingFilter, visibilityFilter]);

  const filteredReviews = useMemo(() => {
    return reviews.filter((review) => {
      if (ratingFilter !== 'all' && review.rating !== Number(ratingFilter)) return false;
      if (visibilityFilter === 'visible' && !review.isVisible) return false;
      if (visibilityFilter === 'hidden' && review.isVisible) return false;
      return true;
    });
  }, [reviews, ratingFilter, visibilityFilter]);

  const handleConfirmHide = async () => {
    if (!confirmHideId) return;

    try {
      await reviewService.toggleVisibility(confirmHideId, false);
      setConfirmHideId(null);
      showSuccess('Đã ẩn đánh giá.');
      await fetchReviews();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const handleShow = async (id: string) => {
    try {
      await reviewService.toggleVisibility(id, true);
      showSuccess('Đã hiển thị đánh giá.');
      await fetchReviews();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const total = filteredReviews.length;
  const totalPages = Math.max(1, Math.ceil(total / PAGE_SIZE));

  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  const paginatedReviews = useMemo(
    () => filteredReviews.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [filteredReviews, page]
  );

  const columns: TableColumn<ReviewListItem>[] = [
    {
      key: 'customerName',
      header: 'Khách hàng',
      width: '20%',
      render: (review) => (
        <div className={styles.customerCell}>
          <div className={styles.customerAvatar}>
            {review.customerName ? review.customerName[0].toUpperCase() : 'C'}
          </div>
          <strong>{review.customerName}</strong>
        </div>
      ),
    },
    {
      key: 'rating',
      header: 'Đánh giá',
      width: '15%',
      render: (review) => (
        <span className={styles.starRating} title={`${review.rating}/5`}>
          {'★'.repeat(review.rating)}
          {'☆'.repeat(5 - review.rating)}
        </span>
      ),
    },
    {
      key: 'comment',
      header: 'Nội dung nhận xét',
      width: '28%',
      render: (review) => (
        <span className={styles.commentText}>
          {review.comment || <em>Không có nhận xét</em>}
        </span>
      ),
    },
    {
      key: 'status',
      header: 'Hiển thị',
      width: '13%',
      render: (review) => (
        <Badge variant={review.isVisible ? 'success' : 'default'}>
          {review.isVisible ? 'Đang hiển thị' : 'Đã ẩn'}
        </Badge>
      ),
    },
    {
      key: 'createdAt',
      header: 'Ngày gửi',
      width: '12%',
      render: (review) => new Date(review.createdAt).toLocaleDateString('vi-VN'),
    },
    {
      key: 'isVisible',
      header: 'Thao tác',
      width: '12%',
      render: (review) =>
        review.isVisible ? (
          <Button
            variant="danger"
            size="sm"
            onClick={(event) => {
              event.stopPropagation();
              setConfirmHideId(review.id);
            }}
          >
            Ẩn đánh giá
          </Button>
        ) : (
          <Button
            variant="primary"
            size="sm"
            onClick={(event) => {
              event.stopPropagation();
              void handleShow(review.id);
            }}
          >
            Hiển thị
          </Button>
        ),
    },
  ];

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.eyebrow}>ĐÁNH GIÁ & Ý KIẾN</span>
        <h1 className={styles.title}>Quản lý đánh giá khách hàng</h1>
        <p className={styles.subtitle}>
          Kiểm duyệt, theo dõi độ hài lòng và ẩn/hiện nhận xét của khách hàng trên hệ thống.
        </p>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <div className={styles.filterCard}>
        <Select
          options={RATING_OPTIONS}
          placeholder="-- Tất cả số sao --"
          value={ratingFilter === 'all' ? '' : ratingFilter}
          onChange={(e) => setRatingFilter(e.target.value || 'all')}
        />
        <Select
          options={[
            { value: 'visible', label: 'Đang hiển thị' },
            { value: 'hidden', label: 'Đã ẩn' },
          ]}
          placeholder="-- Tất cả trạng thái --"
          value={visibilityFilter === 'all' ? '' : visibilityFilter}
          onChange={(e) =>
            setVisibilityFilter((e.target.value || 'all') as 'all' | 'visible' | 'hidden')
          }
        />
        {(ratingFilter !== 'all' || visibilityFilter !== 'all') && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setRatingFilter('all');
              setVisibilityFilter('all');
            }}
          >
            Xóa lọc
          </Button>
        )}
      </div>

      <Table
        columns={columns}
        data={paginatedReviews}
        keyExtractor={(review) => review.id}
        isLoading={isLoading}
        emptyMessage="Chưa có đánh giá nào phù hợp với bộ lọc."
      />

      <Pagination page={page} pageSize={PAGE_SIZE} total={total} onPageChange={setPage} />

      <ConfirmDialog
        isOpen={!!confirmHideId}
        title="Ẩn đánh giá"
        message="Đánh giá này sẽ không còn hiển thị công khai. Bạn có thể hiện lại bất cứ lúc nào."
        isDangerous
        onConfirm={handleConfirmHide}
        onCancel={() => setConfirmHideId(null)}
      />
    </div>
  );
}
