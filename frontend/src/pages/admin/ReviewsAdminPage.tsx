import { useCallback, useEffect, useState } from 'react';

import { Button, ConfirmDialog, ErrorMessage, Pagination, Table } from '../../components/common';

import type { TableColumn } from '../../components/common/Table';

import { reviewService } from '../../services/reviewService';
import type { ReviewListItem } from '../../types/review.types';

import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

import styles from './ReviewsAdminPage.module.css';

export default function ReviewsAdminPage() {
  const { showSuccess } = useToast();

  const [reviews, setReviews] = useState<ReviewListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [confirmHideId, setConfirmHideId] = useState<string | null>(null);

  const [page, setPage] = useState(1);
  const pageSize = 10;

  const fetchReviews = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await reviewService.getAdminReviews();
      setReviews(response.items);
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchReviews();
  }, [fetchReviews]);

  const handleConfirmHide = async () => {
    if (!confirmHideId) return;

    try {
      await reviewService.toggleVisibility(confirmHideId, false);
      setConfirmHideId(null);
      showSuccess('Đã ẩn đánh giá.');
      fetchReviews();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const handleShow = async (id: string) => {
    try {
      await reviewService.toggleVisibility(id, true);
      showSuccess('Đã hiển thị đánh giá.');
      fetchReviews();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    }
  };

  const total = reviews.length;
  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  useEffect(() => {
    if (page > totalPages) {
      setPage(totalPages);
    }
  }, [page, totalPages]);

  const paginatedReviews = reviews.slice((page - 1) * pageSize, page * pageSize);

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
      width: '16%',
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
      width: '32%',
      render: (review) => (
        <span className={styles.commentText}>
          {review.comment || <em>Không có nhận xét</em>}
        </span>
      ),
    },
    {
      key: 'createdAt',
      header: 'Ngày gửi',
      width: '16%',
      render: (review) => new Date(review.createdAt).toLocaleDateString('vi-VN'),
    },
    {
      key: 'isVisible',
      header: 'Thao tác',
      width: '16%',
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
              handleShow(review.id);
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
        <p className={styles.subtitle}>Kiểm duyệt, theo dõi độ hài lòng và ẩn/hiện nhận xét của khách hàng trên hệ thống.</p>
      </div>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <Table
        columns={columns}
        data={paginatedReviews}
        keyExtractor={(review) => review.id}
        isLoading={isLoading}
        emptyMessage="Chưa có đánh giá nào từ khách hàng."
      />

      <Pagination page={page} pageSize={pageSize} total={total} onPageChange={setPage} />

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

