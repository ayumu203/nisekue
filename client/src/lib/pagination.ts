export type PaginationOptions = {
  page?: number
  pageSize?: number
  limit?: number
}

export function applyPaginationSearchParams(url: URL, options?: PaginationOptions): void {
  if (!options) {
    return
  }

  if (options.page !== undefined) {
    url.searchParams.set('page', String(options.page))
  }

  if (options.pageSize !== undefined) {
    url.searchParams.set('pageSize', String(options.pageSize))
  }

  if (options.limit !== undefined) {
    url.searchParams.set('limit', String(options.limit))
  }
}
