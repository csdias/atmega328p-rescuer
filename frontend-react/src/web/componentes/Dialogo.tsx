import { useEffect, useRef, type ReactNode } from 'react';

/**
 * Diálogo modal sobre o <dialog> nativo: traz o foco preso, o Escape e o backdrop sem
 * os reimplementar.
 *
 * `aoFechar` corre também no Escape — quem fecha pelo teclado tomou a mesma decisão que
 * quem carrega em Cancelar, e um diálogo destes fechado sem resposta deixaria a bancada
 * à espera de uma escolha que nunca chega.
 */
export function Dialogo({
  aberto,
  titulo,
  icone,
  aoFechar,
  acoes,
  children,
}: {
  aberto: boolean;
  titulo: string;
  icone?: string;
  aoFechar: () => void;
  acoes: ReactNode;
  children: ReactNode;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialogo = ref.current;
    if (!dialogo) return;

    if (aberto && !dialogo.open) dialogo.showModal();
    else if (!aberto && dialogo.open) dialogo.close();
  }, [aberto]);

  return (
    <dialog ref={ref} className="dialogo" onCancel={(e) => { e.preventDefault(); aoFechar(); }}>
      <h2 className="dialogo__titulo">
        {icone && <span aria-hidden="true">{icone}</span>}
        {titulo}
      </h2>

      <div className="pilha">{children}</div>

      <div className="dialogo__acoes">{acoes}</div>
    </dialog>
  );
}
