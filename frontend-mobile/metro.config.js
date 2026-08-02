// Metro is the React Native bundler — the equivalent of Vite on the web side.
//
// By default it only looks inside this folder, and the bench logic lives one level up,
// in frontend-shared. These three lines tell it where to look; without them
// `import '@atmegapesta/shared'` does not resolve.
const { getDefaultConfig } = require('expo/metro-config');
const path = require('path');

const appRoot = __dirname;
const repoRoot = path.resolve(appRoot, '..');

const config = getDefaultConfig(appRoot);

// Watch the whole repository: a change in frontend-shared reloads the app.
config.watchFolders = [repoRoot];

// The packages are hoisted to the repository root (npm workspaces), not here.
config.resolver.nodeModulesPaths = [
  path.resolve(appRoot, 'node_modules'),
  path.resolve(repoRoot, 'node_modules'),
];

// Without this, Metro would walk up the tree looking for node_modules and could find a
// second copy of React — two copies of React break the hooks silently.
config.resolver.disableHierarchicalLookup = true;

module.exports = config;
