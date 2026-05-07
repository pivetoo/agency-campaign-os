import { Compass } from 'lucide-react'
import { useProductTour } from '../../hooks/useProductTour'
import { useTour } from './TourContext'

export default function TourButton() {
  const { openTour } = useTour()
  const { completed } = useProductTour()

  return (
    <button
      type="button"
      onClick={openTour}
      className="inline-flex items-center gap-2 rounded-full border border-sky-300 bg-sky-50 px-3 py-1.5 text-xs font-medium text-sky-700 hover:bg-sky-100 dark:border-sky-800 dark:bg-sky-950/30 dark:text-sky-300"
    >
      <Compass size={12} />
      {completed ? 'Refazer tour' : 'Tour pelo sistema'}
    </button>
  )
}
