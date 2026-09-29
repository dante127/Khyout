import { request } from './client';
import type { Paged } from './types';
import type { UnitOfMeasure } from './catalog';

export type RfqStatus = 'Open' | 'Awarded' | 'Cancelled' | 'Expired';

export interface RfqSummary {
  id: string;
  title: string;
  categoryId: string;
  categoryName: string;
  quantityNeeded: number;
  unitOfMeasure: UnitOfMeasure;
  closingDate: string;
  status: RfqStatus;
  bidCount: number;
  createdAt: string;
}

export interface RfqDetail {
  id: string;
  buyerCompanyId: string;
  buyerCompanyName: string;
  categoryId: string;
  categoryName: string;
  title: string;
  description: string | null;
  quantityNeeded: number;
  unitOfMeasure: UnitOfMeasure;
  targetDeliveryDate: string;
  closingDate: string;
  status: RfqStatus;
  acceptedQuotationId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface MyQuotation {
  quotationId: string;
  unitPrice: number;
  currency: string;
  validUntil: string;
  leadTimeDays: number;
  note: string | null;
  status: string;
}

export interface RfqBid {
  quotationId: string;
  supplierCompanyId: string;
  supplierName: string;
  unitPrice: number;
  currency: string;
  validUntil: string;
  leadTimeDays: number;
  note: string | null;
  status: string;
  createdAt: string;
}

export interface RfqDetailResult {
  rfq: RfqDetail;
  isBuyerOwner: boolean;
  canReceiveBids: boolean;
  bidCount: number;
  myQuotation: MyQuotation | null;
}

export interface CreateRfqInput {
  categoryId: string;
  title: string;
  description?: string | null;
  quantityNeeded: number;
  unitOfMeasure: UnitOfMeasure;
  /** yyyy-MM-dd (DateOnly on the server). */
  targetDeliveryDate: string;
  /** ISO-8601 timestamp. */
  closingDate: string;
}

export interface SubmitQuotationInput {
  unitPrice: number;
  currency: string;
  validUntil: string;
  leadTimeDays: number;
  note?: string | null;
}

export function createRfq(input: CreateRfqInput): Promise<RfqDetail> {
  return request<RfqDetail>('/api/v1/rfqs', { method: 'POST', body: input });
}

export function getMyRfqs(
  params: { pageNumber?: number; pageSize?: number; status?: RfqStatus } = {},
): Promise<Paged<RfqSummary>> {
  const query = new URLSearchParams();
  if (params.status) query.set('status', params.status);
  if (params.pageNumber !== undefined) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize !== undefined) query.set('pageSize', String(params.pageSize));
  const serialized = query.toString();
  return request<Paged<RfqSummary>>(`/api/v1/rfqs/mine${serialized ? `?${serialized}` : ''}`);
}

export function getRfq(id: string): Promise<RfqDetailResult> {
  return request<RfqDetailResult>(`/api/v1/rfqs/${id}`);
}

export function closeRfq(id: string): Promise<null> {
  return request<null>(`/api/v1/rfqs/${id}/close`, { method: 'POST' });
}

export function extendRfq(id: string, newClosingDate: string): Promise<null> {
  return request<null>(`/api/v1/rfqs/${id}/extend`, {
    method: 'POST',
    body: { newClosingDate },
  });
}

export function submitQuotation(rfqId: string, input: SubmitQuotationInput): Promise<MyQuotation> {
  return request<MyQuotation>(`/api/v1/rfqs/${rfqId}/quotations`, { method: 'POST', body: input });
}

export function getRfqBids(
  rfqId: string,
  params: { pageNumber?: number; pageSize?: number } = {},
): Promise<Paged<RfqBid>> {
  const query = new URLSearchParams();
  if (params.pageNumber !== undefined) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize !== undefined) query.set('pageSize', String(params.pageSize));
  const serialized = query.toString();
  return request<Paged<RfqBid>>(`/api/v1/rfqs/${rfqId}/quotations${serialized ? `?${serialized}` : ''}`);
}
