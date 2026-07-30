import type { Catalog, TestState, TestResult } from '@atmegapesta/shared';

/**
 * The integrity check's tests, with the pins each one exercises.
 *
 * With no results coming from the API, the catalog is shown as pending: it is what really
 * exists — the list of planned tests — without pretending any of them ran.
 */
export function TestTable({
  catalog,
  results,
}: {
  catalog: Catalog;
  results: TestResult[] | null;
}) {
  const porNome = new Map(results?.map((r) => [r.name, r]) ?? []);

  return (
    <table className="table">
      <thead>
        <tr>
          <th scope="col">Teste</th>
          <th scope="col">Pinos</th>
          <th scope="col">Tempo</th>
          <th scope="col">Estado</th>
        </tr>
      </thead>
      <tbody>
        {catalog.tests.map((test) => {
          const result = porNome.get(test.name);
          const state = result?.state ?? 'pending';

          return (
            <tr key={test.name}>
              <td>{test.name}</td>
              <td className="mono subtle">{test.pins.join(' ')}</td>
              <td className="mono">{result?.time ?? 'n/d'}</td>
              <td>
                <span className={`badge badge--${state}`}>{palavra(state)}</span>
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

function palavra(state: TestState): string {
  switch (state) {
    case 'passed':
      return 'Passou';
    case 'failed':
      return 'Falhou';
    case 'pending':
      return 'Pendente';
  }
}
