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
      className="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3 py-1.5 text-xs font-medium text-primary hover:bg-primary/15"
    >
      <Compass size={12} />
      {completed ? 'Refazer tour' : 'Tour pelo sistema'}
    </button>
  )
}
