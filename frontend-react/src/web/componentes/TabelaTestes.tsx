import type { Catalogo, EstadoTeste, ResultadoTeste } from '@atmegapesta/partilhado';

/**
 * Os testes da verificação de integridade, com os pinos que cada um exercita.
 *
 * Sem resultados vindos da API, mostra-se o catálogo em pendente: é o que existe de
 * verdade — a lista dos testes previstos — sem fingir que algum correu.
 */
export function TabelaTestes({
  catalogo,
  resultados,
}: {
  catalogo: Catalogo;
  resultados: ResultadoTeste[] | null;
}) {
  const porNome = new Map(resultados?.map((r) => [r.nome, r]) ?? []);

  return (
    <table className="tabela">
      <thead>
        <tr>
          <th scope="col">Teste</th>
          <th scope="col">Pinos</th>
          <th scope="col">Tempo</th>
          <th scope="col">Estado</th>
        </tr>
      </thead>
      <tbody>
        {catalogo.testes.map((teste) => {
          const resultado = porNome.get(teste.nome);
          const estado = resultado?.estado ?? 'pendente';

          return (
            <tr key={teste.nome}>
              <td>{teste.nome}</td>
              <td className="mono subtil">{teste.pinos.join(' ')}</td>
              <td className="mono">{resultado?.tempo ?? 'n/d'}</td>
              <td>
                <span className={`badge badge--${estado}`}>{palavra(estado)}</span>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

function palavra(estado: EstadoTeste): string {
  switch (estado) {
    case 'passou':
      return 'Passou';
    case 'falhou':
      return 'Falhou';
    case 'pendente':
      return 'Pendente';
  }
}
