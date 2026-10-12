import { useEffect, useRef, useState, type KeyboardEvent, type ReactNode } from 'react';
import './InlineConfirmButton.css';

export const INLINE_CONFIRM_TIMEOUT_MS = 6000;

type InlineConfirmButtonProps = {
  /** Accessible name of the first (destructive) button. Keep it identical to the control's existing name. */
  label: string;
  /** Accessible name of the confirming button. Must not be a substring-match of `label`, so use "Yes, remove …". */
  confirmLabel: string;
  /** Visible content of the first button, such as an icon or text. */
  children: ReactNode;
  className?: string;
  confirmClassName?: string;
  title?: string;
  disabled?: boolean;
  /** Optional sentence shown while armed, announced politely to assistive technology. */
  warning?: string;
  onConfirm: () => void | Promise<unknown>;
};

/**
 * A destructive button that needs a second click. The first click arms it, showing Confirm and Cancel;
 * the armed state clears after INLINE_CONFIRM_TIMEOUT_MS. Escape cancels only while armed.
 */
export function InlineConfirmButton({
  label,
  confirmLabel,
  children,
  className,
  confirmClassName = 'secondary-button',
  title,
  disabled = false,
  warning,
  onConfirm,
}: InlineConfirmButtonProps) {
  const [armed, setArmed] = useState(false);
  const firstRef = useRef<HTMLButtonElement>(null);
  const confirmRef = useRef<HTMLButtonElement>(null);
  const returnFocusOnDisarm = useRef(false);

  useEffect(() => {
    if (!armed) {
      if (returnFocusOnDisarm.current) {
        returnFocusOnDisarm.current = false;
        firstRef.current?.focus();
      }
      return undefined;
    }
    confirmRef.current?.focus();
    const timer = window.setTimeout(() => setArmed(false), INLINE_CONFIRM_TIMEOUT_MS);
    return () => window.clearTimeout(timer);
  }, [armed]);

  const cancel = () => {
    returnFocusOnDisarm.current = true;
    setArmed(false);
  };

  const confirm = () => {
    setArmed(false);
    void onConfirm();
  };

  const handleKeyDown = (event: KeyboardEvent<HTMLSpanElement>) => {
    if (event.key !== 'Escape') return;
    // Stop the event here so Escape cancels the confirmation without also closing the launcher.
    event.preventDefault();
    event.stopPropagation();
    cancel();
  };

  if (!armed) {
    return (
      <button ref={firstRef} className={className} type="button" aria-label={label} title={title} disabled={disabled}
        onClick={() => setArmed(true)}>
        {children}
      </button>
    );
  }

  return (
    <span className="inline-confirm" role="group" aria-live="polite" onKeyDown={handleKeyDown}>
      {warning && <span className="inline-confirm-warning">{warning}</span>}
      <button ref={confirmRef} className={confirmClassName} type="button" aria-label={confirmLabel} disabled={disabled} onClick={confirm}>
        Yes
      </button>
      <button className="secondary-button" type="button" onClick={cancel}>Cancel</button>
    </span>
  );
}
