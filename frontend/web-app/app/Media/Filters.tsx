import { Button, ButtonGroup } from 'flowbite-react';
import { useParams } from 'next/navigation';
import React from 'react'
import { useParamsStore } from '../hooks/useMediaStore';

import { AiFillCamera, AiOutlineClockCircle, AiOutlineSortAscending } from 'react-icons/ai';
import { BsFillStopCircleFill, BsStopwatchFill } from 'react-icons/bs';
import { GiFinishLine, GiFlame } from 'react-icons/gi';

const pageSizeButtons = [4, 8, 12];

const orderButtons = [
    {
        label: 'Show title',
        icon: AiOutlineSortAscending,
        value: 'showtitle'
    },
    {
        label: 'Episode title',
        icon: AiFillCamera,
        value: 'episodetitle'
    },
    {
        label: 'Recently added',
        icon: BsFillStopCircleFill,
        value: 'recent'
    },
]

const filterButtons = [
    {
        label: 'Alphabetical',
        icon: AiOutlineSortAscending,
        value: 'showtitle'
    },
    {
        label: 'Movies',
        icon: AiFillCamera,
        value: 'movies'
    },
    {
        label: 'Recently added',
        icon: BsFillStopCircleFill,
        value: 'recent'
    },
]

export default function Filters() {
    const pageSize = useParamsStore(state => state.pageSize);
    const setParams = useParamsStore(state => state.setParams);
    const orderBy = useParamsStore(state => state.orderBy);
    const filterBy = useParamsStore(state => state.filterBy);

    return (
        <div className='flex justify-between items-center mb-4'>

            <div>
                <span className='uppercase text-sm text-gray-500 mr-2'>Filter by</span>
                <Button.Group>
                    {filterButtons.map(({ label, icon: Icon, value }) => (
                        <Button
                            key={value}
                            onClick={() => setParams({ filterBy: value })}
                            color={`${filterBy === value ? 'red' : 'gray'}`}
                        >
                            <Icon className='mr-3 h-4 w-4' />
                            {label}
                        </Button>
                    ))}
                </Button.Group>
            </div>

            <div>
                <span className='uppercase text-sm text-gray-500 mr-2'>Order by</span>
                <Button.Group>
                    {orderButtons.map(({ label, icon: Icon, value }) => (
                        <Button
                            key={value}
                            onClick={() => setParams({ orderBy: value })}
                            color={`${orderBy === value ? 'red' : 'gray'}`}
                        >
                            <Icon className='mr-3 h-4 w-4' />
                            {label}
                        </Button>
                    ))}
                </Button.Group>
            </div>


            <div>
                <span className='uppercase text-sm text-gray-500 mr-2'>Page Size</span>
                <ButtonGroup>
                    {pageSizeButtons.map((value, i) => (
                        <Button key={i}
                            onClick={() => setParams({ pageSize: value })}
                            color={`${pageSize === value ? 'red' : 'gray'}`}
                            className='focus:ring-0'
                        >
                            {value}
                        </Button>
                    ))}
                </ButtonGroup>
            </div>
        </div>
    )
}