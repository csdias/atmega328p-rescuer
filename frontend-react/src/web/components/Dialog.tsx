import { useEffect, useRef, type ReactNode } from 'react';

/**
 * Modal dialog over the native <dialog>: it brings the focus trap, Escape and the backdrop
 * without reimplementing them.
 *
 * `onClose` also runs on Escape — whoever closes with the keyboard has made the same
 * decision as whoever presses Cancel, and one of these dialogs closed without an answer
 * would leave the bench waiting for a choice that never arrives.
 */
export function Dialog({
  open,
  title,
  icon,
  onClose,
  actions,
  children,
}: {
  open: boolean;
  title: string;
  icon?: string;
  onClose: () => void;
  actions: ReactNode;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;

    if (open && !dialog.open) dialog.showModal();
    else if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog ref={ref} className="dialog" onCancel={(e) => { e.preventDefault(); onClose(); }}>
      <h2 className="dialog__title">
        {icon && <span aria-hidden="true">{icon}</span>}
        {title}
      </h2>

      <div className="stack">{children}</div>

      <div className="dialog__actions">{actions}</div>
    </dialog>
  );
}
