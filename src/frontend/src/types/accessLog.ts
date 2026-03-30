export interface AccessLogDto {
  id: string;
  memberId: string;
  memberName: string;
  performedByUserId: string;
  performedByUserName: string;
  result: 'Allowed' | 'Denied';
  denialReason?: string;
  createdAt: string; // ISO date string
  clientGuid: string;
}

export interface AccessLogFilter {
  fromDate?: string;
  toDate?: string;
  performedByUserId?: string;
  memberId?: string;
  result?: 'Allowed' | 'Denied' | '';
  page: number;
  pageSize: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}