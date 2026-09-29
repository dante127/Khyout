import { request } from './client';
import type { Paged } from './types';

export type SampleStatus = 'Requested' | 'Approved' | 'Shipped' | 'Received' | 'Rejected';

export interface Sample {
  id: string;
  buyerCompanyId: string;
  buyerCompanyName: string;
  supplierCompanyId: string;
  supplierCompanyName: string;
  productId: string;
  productTitle: string;
  rfqQuotationId: string | null;
  status: SampleStatus;
  quantity: number;
  deliveryCity: string | null;
  note: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSampleInput {
  productId: string;
  quantity: number;
  deliveryCity?: string | null;
  note?: string | null;
  rfqQuotationId?: string | null;
}

export function createSampleRequest(input: CreateSampleInput): Promise<Sample> {
  return request<Sample>('/api/v1/samples', { method: 'POST', body: input });
}

export function listSamples(
  params: { pageNumber?: number; pageSize?: number; status?: SampleStatus } = {},
): Promise<Paged<Sample>> {
  const query = new URLSearchParams();
  if (params.status) query.set('status', params.status);
  if (params.pageNumber !== undefined) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize !== undefined) query.set('pageSize', String(params.pageSize));
  const serialized = query.toString();
  return request<Paged<Sample>>(`/api/v1/samples${serialized ? `?${serialized}` : ''}`);
}

/** Supplier (or admin) action: moves a sample request through its lifecycle. */
export function updateSampleStatus(id: string, status: SampleStatus): Promise<Sample> {
  return request<Sample>(`/api/v1/samples/${id}/status`, { method: 'PATCH', body: { status } });
}
