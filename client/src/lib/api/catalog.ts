import { request } from './client';
import type { Paged } from './types';

export type UnitOfMeasure = 'Meter' | 'Kg' | 'Roll' | 'Yard';
export type WeaveStructure = 'Plain' | 'Twill' | 'Satin' | 'Knit' | 'Denim';
export type FiberType =
  | 'Cotton'
  | 'Lycra'
  | 'Viscose'
  | 'Polyester'
  | 'Wool'
  | 'Linen'
  | 'Silk'
  | 'Nylon'
  | 'Other';

export interface ProductSummary {
  id: string;
  title: string;
  categoryId: string;
  categoryName: string;
  moq: number;
  unitOfMeasure: UnitOfMeasure;
  indicativePrice: number | null;
  currency: string | null;
  gsm: number | null;
  supplierCompanyId: string;
  supplierName: string;
  supplierCity: string;
  createdAt: string;
}

export interface FiberSpec {
  fiberType: FiberType;
  percentage: number;
}

export interface ProductImage {
  id: string;
  storagePath: string;
  widthPx: number;
  heightPx: number;
  sortOrder: number;
}

export interface ProductDetail {
  id: string;
  title: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  moq: number;
  unitOfMeasure: UnitOfMeasure;
  indicativePrice: number | null;
  currency: string | null;
  status: string;
  gsm: number | null;
  gsmTolerancePct: number | null;
  weaveStructure: WeaveStructure | null;
  widthCm: number | null;
  colorFamily: string | null;
  weightPerMeterG: number | null;
  careNotes: string | null;
  composition: FiberSpec[];
  images: ProductImage[];
  supplierCompanyId: string;
  supplierName: string;
  supplierCity: string;
  createdAt: string;
  updatedAt: string;
}

export interface CategoryNode {
  id: string;
  slug: string;
  nameAr: string;
  nameEn: string;
  sortOrder: number;
  children: CategoryNode[];
}

export interface ProductSearchParams {
  categoryId?: string;
  gsmMin?: number;
  gsmMax?: number;
  fibers?: string[];
  moqMax?: number;
  city?: string;
  sort?: 'newest' | 'moqAsc' | 'gsmAsc';
  pageNumber?: number;
  pageSize?: number;
}

export type ProductStatusValue = 'Draft' | 'Active' | 'Archived';

export interface CreateProductInput {
  categoryId: string;
  title: string;
  description?: string | null;
  moq: number;
  unitOfMeasure: UnitOfMeasure;
  indicativePrice?: number | null;
  currency?: string | null;
  gsm: number;
  gsmTolerancePct: number;
  weaveStructure: WeaveStructure;
  widthCm: number;
  colorFamily?: string | null;
  weightPerMeterG?: number | null;
  careNotes?: string | null;
  composition: FiberSpec[];
}

/** PUT requires the full payload plus the desired status (publish = status Active). */
export type UpdateProductInput = CreateProductInput & { status: ProductStatusValue };

function toQueryString(params: ProductSearchParams): string {
  const query = new URLSearchParams();
  if (params.categoryId) query.set('categoryId', params.categoryId);
  if (params.gsmMin !== undefined) query.set('gsmMin', String(params.gsmMin));
  if (params.gsmMax !== undefined) query.set('gsmMax', String(params.gsmMax));
  if (params.moqMax !== undefined) query.set('moqMax', String(params.moqMax));
  if (params.city) query.set('city', params.city);
  if (params.sort) query.set('sort', params.sort);
  if (params.pageNumber !== undefined) query.set('pageNumber', String(params.pageNumber));
  if (params.pageSize !== undefined) query.set('pageSize', String(params.pageSize));
  for (const fiber of params.fibers ?? []) {
    query.append('fibers', fiber);
  }
  const serialized = query.toString();
  return serialized ? `?${serialized}` : '';
}

export function searchProducts(params: ProductSearchParams = {}): Promise<Paged<ProductSummary>> {
  return request<Paged<ProductSummary>>(`/api/v1/products${toQueryString(params)}`);
}

export function getProduct(id: string): Promise<ProductDetail> {
  return request<ProductDetail>(`/api/v1/products/${id}`);
}

export function getCategoryTree(): Promise<CategoryNode[]> {
  return request<CategoryNode[]>('/api/v1/categories');
}

export function createProduct(input: CreateProductInput): Promise<ProductDetail> {
  return request<ProductDetail>('/api/v1/products', { method: 'POST', body: input });
}

export function updateProduct(id: string, input: UpdateProductInput): Promise<ProductDetail> {
  return request<ProductDetail>(`/api/v1/products/${id}`, { method: 'PUT', body: input });
}

export function uploadProductImage(productId: string, file: File): Promise<ProductImage> {
  const form = new FormData();
  form.append('file', file);
  return request<ProductImage>(`/api/v1/products/${productId}/images`, {
    method: 'POST',
    body: form,
    isFormData: true,
  });
}
