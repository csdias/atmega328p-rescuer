// O Metro é o empacotador do React Native — o equivalente ao Vite do lado web.
//
// Por omissão só olha para dentro desta pasta, e a lógica da bancada vive um nível acima,
// em frontend-shared. Estas três linhas dizem-lhe onde procurar; sem elas o
// `import '@atmegapesta/partilhado'` não resolve.
const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const raizApp = __dirname;
const raizRepo = path.resolve(raizApp, '..');

const config = getDefaultConfig(raizApp);

// Vigiar o repositório todo: uma alteração em frontend-shared recarrega a app.
config.watchFolders = [raizRepo];

// Os pacotes estão hasteados na raiz do repositório (npm workspaces), não aqui.
config.resolver.nodeModulesPaths = [
  path.resolve(raizApp, 'node_modules'),
  path.resolve(raizRepo, 'node_modules'),
];

// Sem isto, o Metro subiria a árvore à procura de node_modules e podia encontrar uma
// segunda copia do React — duas copias do React quebram os hooks em silêncio.
config.resolver.disableHierarchicalLookup = true;

module.exports = config;
