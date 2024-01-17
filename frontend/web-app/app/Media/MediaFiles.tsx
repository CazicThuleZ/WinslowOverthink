'use client'

import React, { useEffect, useState } from 'react'
import MediaCard from './MediaCard';
import { MediaFile, PagedResult } from '../types/Index';
import AppPagination from '../components/AppPagination';
import { getData } from '../actions/mediaActions';
import Filters from './Filters';
import { useParamsStore } from '../hooks/useMediaStore';
import { shallow } from 'zustand/shallow';
import qs from 'query-string';
import EmptyFilter from '../components/EmptyFilter';

export default function MediaFiles() {

  const [data, setData] = useState<PagedResult<MediaFile>>();
  const params = useParamsStore(state => ({
    pageNumber: state.pageNumber,
    pageSize: state.pageSize,
    searchTerm: state.searchTerm,
    orderBy: state.orderBy,
    filterBy: state.filterBy,
  }), shallow);

  const setParams = useParamsStore(state => state.setParams);
  const url = qs.stringifyUrl({ url: '', query: params });

  function setPageNumber(pageNumber: number) {
    setParams({ pageNumber });
  }


  useEffect(() => {
    getData(url).then(data => {
      setData(data);
    })
  }, [url])

  if (!data) <h3>Loading...</h3>  

  return (

    <>
      <div>

        <Filters />

        {data?.totalCount === 0 ? (
          <EmptyFilter showReset />
        ) : (
          <>
            <div className='grid grid-cols-4 gap-6'>
              {data?.result.map(mediaFile => (
                <MediaCard mediaFile={mediaFile} key={mediaFile.id} />
              ))}
            </div>

            <div className='flex justify-center mt-4'>
              <AppPagination pageChanged={setPageNumber} currentPage={params.pageNumber} pageCount={data?.pageCount || 0} />
            </div>
          </>
        )}

        {/* {JSON.stringify(data, null, 2)} */}
      </div>
    </>
  )
}
