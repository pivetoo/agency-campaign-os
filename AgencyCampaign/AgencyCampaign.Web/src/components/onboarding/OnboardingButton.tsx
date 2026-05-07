import { useState } from 'react'
import { Sparkles } from 'lucide-react'
import { useOnboarding } from '../../hooks/useOnboarding'
import OnboardingWizard from './OnboardingWizard'

export default function OnboardingButton() {
  const [open, setOpen] = useState(false)
  const onboarding = useOnboarding()
  const isStarted = onboarding.completedCount > 0

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-3 py-1.5 text-xs font-medium text-primary hover:bg-primary/15"
      >
        <Sparkles size={12} />
        {onboarding.isCompleted
          ? 'Onboarding concluído'
          : isStarted
            ? `Onboarding (${onboarding.completedCount}/${onboarding.totalSteps})`
            : 'Onboarding'}
      </button>
      <OnboardingWizard open={open} onOpenChange={setOpen} />
    </>
  )
}
