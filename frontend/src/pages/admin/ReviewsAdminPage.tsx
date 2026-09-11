import { useCallback, useEffect, useState } from 'react';

import { Button, ErrorMessage, Table } from '../../components/common';
import type { TableColumn } from '../../components/common/Table';

import { reviewService } from '../../services/reviewService';
import type { ReviewListItem } from '../../types/review.types';
import { extractApiError } from '../../utils/apiError';
import { useToast } from '../../hooks/useToast';

export default function ReviewsAdminPage() {
  const { showSuccess } = useToast();

  const [reviews, setReviews] = useState<ReviewListItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [togglingId, setTogglingId] = useState<string | null>(null);

  const loadReviews = useCallback(async () => {
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
    loadReviews();
  }, [loadReviews]);

  const handleToggleVisibility = async (review: ReviewListItem) => {
    setTogglingId(review.id);
    setErrorMessage(null);

    try {
      await reviewService.toggleVisibility(review.id, !review.isVisible);

      showSuccess(review.isVisible ? 'Đã ẩn đánh giá.' : 'Đã hiển thị đánh giá.');

      await loadReviews();
    } catch (err) {
      setErrorMessage(extractApiError(err).message);
    } finally {
      setTogglingId(null);
    }
  };

  const columns: TableColumn<ReviewListItem>[] = [
    {
      key: 'customerName',
      header: 'Khách hàng',
      width: '18%',
      render: (review) => review.customerName,
    },
    {
      key: 'rating',
      header: 'Đánh giá',
      width: '15%',
      render: (review) => (
        <span title={`${review.rating}/5`}>
          {'★'.repeat(review.rating)}
          {'☆'.repeat(5 - review.rating)}
        </span>
      ),
    },
    {
      key: 'comment',
      header: 'Nội dung',
      width: '30%',
      render: (review) => review.comment || 'Không có nhận xét',
    },
    {
      key: 'createdAt',
      header: 'Ngày tạo',
      width: '17%',
      render: (review) => new Date(review.createdAt).toLocaleDateString('vi-VN'),
    },
    {
      key: 'isVisible',
      header: 'Trạng thái',
      width: '20%',
      render: (review) => (
        <Button
          size="sm"
          variant={review.isVisible ? 'primary' : 'secondary'}
          isLoading={togglingId === review.id}
          onClick={(event) => {
            event.stopPropagation();
            handleToggleVisibility(review);
          }}
        >
          {review.isVisible ? 'Đang hiển thị' : 'Đang ẩn'}
        </Button>
      ),
    },
  ];

  return (
    <div>
      <h1>Quản lý đánh giá</h1>

      {errorMessage && <ErrorMessage message={errorMessage} />}

      <Table
        columns={columns}
        data={reviews}
        keyExtractor={(review) => review.id}
        isLoading={isLoading}
        emptyMessage="Chưa có đánh giá nào"
      />
    </div>
  );
}
