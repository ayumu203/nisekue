import useSWR from 'swr'

type UsePagedListOptions<T> = {
  enabled: boolean
  keyPrefix: ReadonlyArray<string | number>
  page: number
  fetchPage: (page: number) => Promise<ReadonlyArray<T>>
  dedupingInterval?: number
}

export function usePagedList<T>({
  enabled,
  keyPrefix,
  page,
  fetchPage,
  dedupingInterval,
}: UsePagedListOptions<T>) {
  const currentPageKey = enabled ? [...keyPrefix, page] : null
  const nextPageKey = enabled ? [...keyPrefix, page + 1] : null

  const {
    data: currentPageItems,
    error,
    isLoading,
  } = useSWR(currentPageKey, () => fetchPage(page), { dedupingInterval })

  const {
    data: nextPageItems,
    error: nextPageError,
    isLoading: isNextPageLoading,
  } = useSWR(nextPageKey, () => fetchPage(page + 1), { dedupingInterval })

  const hasNextPage = !isNextPageLoading && nextPageError == null && (nextPageItems?.length ?? 0) > 0

  return {
    data: currentPageItems,
    error,
    isLoading,
    hasNextPage,
    isNextPageLoading,
  }
}
