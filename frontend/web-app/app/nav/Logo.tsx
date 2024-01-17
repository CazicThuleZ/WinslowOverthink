'use client'

import { useParamsStore } from '../hooks/useMediaStore';
import { usePathname, useRouter } from 'next/navigation'
import React from 'react'
import { GiMonoWheelRobot } from 'react-icons/gi'

export default function Logo() {
    const router = useRouter();
    const pathname = usePathname();
    const reset = useParamsStore(state => state.reset);

    function doReset() {
        if (pathname !== '/') router.push('/');
        reset();
    }

    return (
        <div onClick={doReset} className='cursor-pointer flex items-center gap-2 text-3xl font-semibold text-red-500'>
            <GiMonoWheelRobot size={34} />
            <div>Winslow Overthink</div>
        </div>
    )
}