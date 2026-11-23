import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const ROOT_DIR = path.resolve(__dirname, '..');

const REQUIRED_F0_FILES = [
  '.github/workflows/ci.yml',
  '.github/CODEOWNERS',
  '.github/PULL_REQUEST_TEMPLATE.md',
  '.github/dependabot.yml',
  'apps/desktop/README.md',
  'engine/README.md',
  'catalog/README.md',
  'schemas/README.md',
  'schemas/action.schema.json',
  'docs/PRINCIPLES.md',
  'docs/architecture/OVERVIEW.md',
  'docs/adr/README.md',
  'docs/adr/0001-monorepo-and-stack.md',
  'docs/adr/0002-privilege-separation.md',
  'docs/adr/0003-elevated-ipc-channel.md',
  'docs/adr/0004-approval-token.md',
  'docs/adr/0005-storage-and-evidence-chain.md',
  'docs/adr/0006-restoration-strategy.md',
  'docs/adr/0007-optimization-intelligence.md',
  'docs/adr/0008-adaptive-core-authority.md',
  'docs/adr/0009-component-classification.md',
  'docs/adr/0010-reboot-transactions-and-recovery-guard.md',
  'docs/adr/0011-usage-observation-privacy.md',
  'docs/adr/0012-versioned-closed-catalog.md',
  'docs/adr/0013-consent-levels-and-expert-mode.md',
  'docs/adr/0014-provisioning-and-image-out-of-core.md',
  'docs/adr/0015-source-and-reference-policy.md',
  'docs/adr/0016-user-catalog-extensions.md',
  'docs/adr/0017-user-scope-in-elevated-executor.md',
  'docs/security/THREAT_MODEL.md',
  'docs/security/SAFETY_RULES.md',
  'docs/intelligence/OPTIMIZATION_INTELLIGENCE.md',
  'docs/catalog/CATALOG_SPEC.md',
  'docs/catalog/CATEGORIES.md',
  'docs/catalog/REFERENCE_POLICY.md',
  'docs/process/CHECKPOINTS.md',
  'docs/process/DEFINITION_OF_DONE.md',
  'docs/process/DECISIONS_PENDING.md',
  'tools/check-docs.mjs',
  'tools/check-docs.selftest.mjs',
  '.editorconfig',
  '.gitignore',
  'package.json',
  'README.md',
  'SECURITY.md'
];

const ALLOWED_STAGE2_FILES = new Set([
  'pnpm-workspace.yaml',
  'pnpm-lock.yaml',
  'docs/process/DEPENDENCIES.md',
  'docs/architecture/RUNTIME_REQUIREMENTS.md',
  'docs/adr/0018-tauri-surface-and-frontend-dependency-policy.md',
  'docs/adr/0019-modules-manifest.md',
  'schemas/modules-manifest.schema.json',
  'docs/adr/0020-ipc-host-readonly-ndjson.md',
  'schemas/ipc-host.schema.json',
  '.gitattributes'
]);

const ALLOWED_ROOT_PKG_KEYS = new Set([
  'name',
  'version',
  'private',
  'description',
  'type',
  'packageManager',
  'engines',
  'scripts'
]);

const ADR_REQUIRED_SECTIONS = [
  '## Estado',
  '## Fecha',
  '## Contexto',
  '## Decisión',
  '## Alternativas descartadas',
  '## Consecuencias',
  '## Límites y riesgos conocidos',
  '## Preguntas abiertas'
];

const EXPECTED_ENUMS = {
  primitive: [
    'registry.setValue',
    'registry.deleteValue',
    'service.setStartType',
    'service.stop',
    'task.setEnabled',
    'startup.setEnabled',
    'power.setValue',
    'network.setDns',
    'network.flushDns',
    'feature.setState',
    'file.cleanup',
    'repair.run',
    'appx.remove'
  ],
  consentLevel: ['standard', 'advanced', 'reinforced', 'expert'],
  componentClass: ['Expendable', 'Optional', 'FeatureDependent', 'Critical', 'Undetermined'],
  reversibility: ['Full', 'Partial', 'None'],
  status: ['draft', 'lab-tested', 'stable', 'deprecated']
};

const FORBIDDEN_SCHEMA_KEYS = new Set([
  'command',
  'commands',
  'script',
  'scripts',
  'powershell',
  'cmd',
  'shell',
  'rawReg',
  'arbitraryPath',
  'freeCommand',
  'rawPath',
  'exec'
]);

const HTTP_PREFIX = 'http' + '://';
const HTTPS_PREFIX = 'https' + '://';

/**
 * Allowlist explícita y mínima para R9-EXTERNAL-URLS.
 * Cada entrada incluye su justificación técnica obligatoria.
 */
const ALLOWED_EXTERNAL_URLS = new Map([
  [
    `${HTTPS_PREFIX}json-schema.org/draft/2020-12/schema`,
    'Identificador estándar del metaesquema JSON Schema Draft 2020-12 en $schema.'
  ],
  [
    `${HTTPS_PREFIX}evo-optimizer.local/schemas/action.schema.json`,
    'URI simbólico local de $id del esquema de acciones de EVO; no es un enlace de red.'
  ],
  [
    `${HTTPS_PREFIX}evo-optimizer.local/schemas/modules-manifest.schema.json`,
    'URI simbólico local de $id del esquema del manifiesto de módulos de EVO; no es un enlace de red.'
  ],
  [
    `${HTTPS_PREFIX}evo-optimizer.local/schemas/ipc-host.schema.json`,
    'URI simbólico local de $id del contrato IPC de solo lectura de EVO; no es un enlace de red.'
  ],
  [
    `${HTTPS_PREFIX}schema.tauri.app/config/2`,
    'Identificador del $schema de configuración de Tauri v2 en tauri.conf.json.'
  ],
  [
    `${HTTP_PREFIX}www.w3.org/2000/svg`,
    'Namespace XML estándar del formato SVG en evo-icon.svg; no es un enlace de red.'
  ],
  [
    `${HTTP_PREFIX}localhost:1420`,
    'URL de loopback local del servidor de desarrollo de Vite en build.devUrl de tauri.conf.json.'
  ]
]);

/**
 * Los lockfiles generados por gestores de paquetes (pnpm-lock.yaml, Cargo.lock)
 * se excluyen de R9-EXTERNAL-URLS porque almacenan por diseño URLs de tarballs
 * y registros de paquetes (registry.npmjs.org, crates.io) necesarios para
 * reproducibilidad e integridad criptográfica.
 */
const LOCKFILE_BASENAMES = new Set(['pnpm-lock.yaml', 'Cargo.lock']);

const BINARY_EXTENSIONS = new Set([
  '.png',
  '.ico',
  '.icns',
  '.jpg',
  '.jpeg',
  '.webp',
  '.gif',
  '.woff',
  '.woff2',
  '.ttf',
  '.eot',
  '.exe',
  '.dll'
]);

const IGNORED_DIRS = new Set(['.git', 'node_modules', 'dist', 'target', 'gen', 'build', 'out', 'coverage']);

const errors = [];

function normalizeRelPath(relPath) {
  return relPath.split(path.sep).join('/').replace(/\\/g, '/');
}

function readTextNormalized(absPath) {
  return fs.readFileSync(absPath, 'utf8').replace(/\r\n/g, '\n');
}

function pass(ruleName, detail) {
  console.log(`[PASS] ${ruleName}: ${detail}`);
}

function fail(ruleName, detail) {
  console.error(`[FAIL] ${ruleName}: ${detail}`);
  errors.push(`${ruleName}: ${detail}`);
}

function collectAllRepoFiles(dir, list = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (IGNORED_DIRS.has(entry.name)) continue;
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      collectAllRepoFiles(fullPath, list);
    } else if (entry.isFile()) {
      list.push(normalizeRelPath(path.relative(ROOT_DIR, fullPath)));
    }
  }
  return list;
}

// R1. Verificar ausencia de LICENSE (pendiente de añadir por el humano con texto oficial GPL-3.0-or-later)
function checkNoLicenseFile() {
  const rootEntries = fs.readdirSync(ROOT_DIR);
  const licenseFiles = rootEntries.filter((name) => /^license(\..+)?$/i.test(name));
  if (licenseFiles.length > 0) {
    fail(
      'R1-NO-LICENSE',
      `Se encontró archivo de licencia (${licenseFiles.join(', ')}); el archivo LICENSE oficial lo añadirá el humano.`
    );
  } else {
    pass('R1-NO-LICENSE', 'No existe archivo LICENSE (pendiente de adición por el humano según D2).');
  }
}

// R2 & R3. Verificar que los 44 archivos F0 existen, solo hay extras autorizados de Etapa 2 y package.json cumple reglas
function checkStructureAndPackageJson() {
  const missing = [];
  for (const relPath of REQUIRED_F0_FILES) {
    const absPath = path.join(ROOT_DIR, relPath);
    if (!fs.existsSync(absPath) || !fs.statSync(absPath).isFile()) {
      missing.push(relPath);
    }
  }

  const requiredSet = new Set(REQUIRED_F0_FILES);
  const actualFiles = collectAllRepoFiles(ROOT_DIR);
  const unauthorizedExtras = actualFiles.filter((f) => {
    if (requiredSet.has(f)) return false;
    if (ALLOWED_STAGE2_FILES.has(f)) return false;
    if (f.startsWith('apps/desktop/')) return false;
    if (f.startsWith('engine/')) return false;
    if (f === 'catalog/actions.json') return false;
    if (/^license(\..+)?$/i.test(path.basename(f))) return false;
    return true;
  });

  if (missing.length > 0) {
    fail('R2-STRUCTURE', `Faltan archivos F0 obligatorios: ${missing.join(', ')}`);
  } else if (unauthorizedExtras.length > 0) {
    fail('R2-STRUCTURE', `Se detectaron archivos no autorizados fuera de F0/Etapa 2: ${unauthorizedExtras.join(', ')}`);
  } else {
    pass(
      'R2-STRUCTURE',
      `Los ${REQUIRED_F0_FILES.length} archivos F0 existen y todos los archivos presentes (${actualFiles.length}) están dentro del alcance permitido.`
    );
  }

  const pkgPath = path.join(ROOT_DIR, 'package.json');
  if (fs.existsSync(pkgPath)) {
    const pkg = JSON.parse(readTextNormalized(pkgPath));
    const invalidKeys = Object.keys(pkg).filter((k) => !ALLOWED_ROOT_PKG_KEYS.has(k));
    let r3Ok = true;

    if (pkg.dependencies || pkg.devDependencies || pkg.peerDependencies || pkg.optionalDependencies) {
      fail('R3-PACKAGE-JSON', 'El package.json raíz no debe declarar dependencies ni devDependencies.');
      r3Ok = false;
    }
    if ('license' in pkg) {
      fail('R3-PACKAGE-JSON', 'El package.json raíz no debe incluir el campo "license" hasta que el humano añada LICENSE (D2).');
      r3Ok = false;
    }
    if (invalidKeys.length > 0) {
      fail('R3-PACKAGE-JSON', `El package.json raíz contiene claves no permitidas: ${invalidKeys.join(', ')}`);
      r3Ok = false;
    }
    if (!pkg.scripts || !pkg.scripts.check || !pkg.scripts.check.includes('node tools/check-docs.mjs')) {
      fail('R3-PACKAGE-JSON', 'El package.json raíz debe mantener el script "check" ejecutando node tools/check-docs.mjs.');
      r3Ok = false;
    }

    const desktopPkgPath = path.join(ROOT_DIR, 'apps/desktop/package.json');
    if (fs.existsSync(desktopPkgPath)) {
      const desktopPkg = JSON.parse(readTextNormalized(desktopPkgPath));
      if ('license' in desktopPkg) {
        fail('R3-PACKAGE-JSON', 'apps/desktop/package.json no debe incluir el campo "license" (D2).');
        r3Ok = false;
      }
    }

    const cargoTomlPath = path.join(ROOT_DIR, 'apps/desktop/src-tauri/Cargo.toml');
    if (fs.existsSync(cargoTomlPath)) {
      const cargoToml = readTextNormalized(cargoTomlPath);
      if (/^\s*license\s*=/m.test(cargoToml)) {
        fail('R3-PACKAGE-JSON', 'apps/desktop/src-tauri/Cargo.toml no debe incluir el campo "license" (D2).');
        r3Ok = false;
      }
    }

    if (r3Ok) {
      pass('R3-PACKAGE-JSON', 'package.json raíz cumple las claves permitidas sin dependencias ni campo license.');
    }
  }
}

// R4 & R5. Verificar ADRs (17 en Commit 3; 18 una vez creado ADR-0018 en Etapa 2)
function checkAdrs() {
  const adrDir = path.join(ROOT_DIR, 'docs/adr');
  const indexPath = path.join(adrDir, 'README.md');
  if (!fs.existsSync(indexPath)) {
    fail('R4-ADR-INDEX', 'No existe docs/adr/README.md.');
    return;
  }

  const indexContent = readTextNormalized(indexPath);
  const adrFiles = fs
    .readdirSync(adrDir)
    .filter((f) => /^\d{4}-.+\.md$/.test(f))
    .sort();

  const hasAdr0018 = fs.existsSync(
    path.join(adrDir, '0018-tauri-surface-and-frontend-dependency-policy.md')
  );
  const hasAdr0019 = fs.existsSync(
    path.join(adrDir, '0019-modules-manifest.md')
  );
  const hasAdr0020 = fs.existsSync(
    path.join(adrDir, '0020-ipc-host-readonly-ndjson.md')
  );
  const expectedCount = hasAdr0020 ? 20 : (hasAdr0019 ? 19 : (hasAdr0018 ? 18 : 17));

  let indexValid = adrFiles.length === expectedCount;
  let templateValid = adrFiles.length === expectedCount;

  if (adrFiles.length !== expectedCount) {
    fail('R4-ADR-INDEX', `Se esperaban exactamente ${expectedCount} ADRs y se encontraron ${adrFiles.length}.`);
  }

  for (let i = 1; i <= expectedCount; i++) {
    const numStr = String(i).padStart(4, '0');
    const matchingFile = adrFiles.find((f) => f.startsWith(`${numStr}-`));
    if (!matchingFile) {
      fail('R4-ADR-INDEX', `Falta el ADR número ${numStr}.`);
      indexValid = false;
      templateValid = false;
      continue;
    }

    if (!indexContent.includes(`| ${numStr} |`) || !indexContent.includes(`(${matchingFile})`)) {
      fail(
        'R4-ADR-INDEX',
        `El ADR ${matchingFile} (número ${numStr}) no coincide en el índice docs/adr/README.md.`
      );
      indexValid = false;
    }

    const adrContent = readTextNormalized(path.join(adrDir, matchingFile));
    if (!adrContent.startsWith(`# ADR-${numStr}:`)) {
      fail('R5-ADR-TEMPLATE', `${matchingFile} no comienza con "# ADR-${numStr}:".`);
      templateValid = false;
    }

    for (let sIdx = 0; sIdx < ADR_REQUIRED_SECTIONS.length; sIdx++) {
      const sectionHeader = ADR_REQUIRED_SECTIONS[sIdx];
      const pos = adrContent.indexOf(sectionHeader);
      if (pos === -1) {
        fail('R5-ADR-TEMPLATE', `${matchingFile} no contiene la sección obligatoria "${sectionHeader}".`);
        templateValid = false;
        continue;
      }

      const nextHeader = ADR_REQUIRED_SECTIONS[sIdx + 1];
      const endPos = nextHeader ? adrContent.indexOf(nextHeader, pos) : adrContent.length;
      const sectionBody = adrContent
        .slice(pos + sectionHeader.length, endPos !== -1 ? endPos : adrContent.length)
        .trim();

      if (sectionBody.length === 0) {
        fail('R5-ADR-TEMPLATE', `${matchingFile} tiene vacía la sección "${sectionHeader}".`);
        templateValid = false;
      }
    }
  }

  const maxStr = String(expectedCount).padStart(4, '0');
  if (indexValid) {
    pass('R4-ADR-INDEX', `Los ${expectedCount} ADRs (0001 a ${maxStr}) están registrados y enlazados en docs/adr/README.md.`);
  }
  if (templateValid) {
    pass('R5-ADR-TEMPLATE', `Los ${expectedCount} ADRs cumplen todas las secciones de la plantilla con "Límites y riesgos conocidos" no vacío.`);
  }
}

// R6 & R10. Comprobaciones estructurales de schemas/action.schema.json (sin validación formal contra metaesquema)
function checkActionSchema() {
  const schemaPath = path.join(ROOT_DIR, 'schemas/action.schema.json');
  if (!fs.existsSync(schemaPath)) {
    fail('R6-SCHEMA', 'No existe schemas/action.schema.json.');
    fail('R10-SCHEMA-STRUCTURE', 'No existe schemas/action.schema.json.');
    return null;
  }

  let schema;
  try {
    schema = JSON.parse(readTextNormalized(schemaPath));
  } catch (err) {
    fail('R6-SCHEMA', `schemas/action.schema.json no es JSON parseable: ${err.message}`);
    fail('R10-SCHEMA-STRUCTURE', `schemas/action.schema.json no es JSON parseable: ${err.message}`);
    return null;
  }

  let r6Ok = true;
  let r10Ok = true;

  if (schema.$schema !== `${HTTPS_PREFIX}json-schema.org/draft/2020-12/schema`) {
    fail('R6-SCHEMA', 'schemas/action.schema.json debe incluir la propiedad $schema esperada.');
    r6Ok = false;
    r10Ok = false;
  }

  if (typeof schema.$comment !== 'string' || !schema.$comment.includes('BORRADOR F0')) {
    fail('R6-SCHEMA', 'schemas/action.schema.json debe incluir "$comment": "BORRADOR F0, no validado contra Windows real".');
    r6Ok = false;
    r10Ok = false;
  }

  if (schema.additionalProperties !== false) {
    fail('R6-SCHEMA', 'schemas/action.schema.json debe tener "additionalProperties": false en el nivel raíz.');
    r6Ok = false;
    r10Ok = false;
  }

  for (const [propName, expectedValues] of Object.entries(EXPECTED_ENUMS)) {
    const actualEnum = schema.properties?.[propName]?.enum;
    if (!Array.isArray(actualEnum)) {
      fail('R6-SCHEMA', `Falta el enum obligatorio en properties.${propName}.`);
      r6Ok = false;
      r10Ok = false;
      continue;
    }
    const missingVals = expectedValues.filter((v) => !actualEnum.includes(v));
    if (missingVals.length > 0 || actualEnum.length !== expectedValues.length) {
      fail(
        'R6-SCHEMA',
        `El enum properties.${propName} no coincide con los valores esperados. Esperado: [${expectedValues.join(', ')}].`
      );
      r6Ok = false;
      r10Ok = false;
    }
  }

  const i18nRequired = schema.properties?.i18n?.required || [];
  if (!i18nRequired.includes('es') || !i18nRequired.includes('en')) {
    fail('R6-SCHEMA', 'properties.i18n debe exigir los idiomas "es" y "en".');
    r6Ok = false;
    r10Ok = false;
  }

  function inspectNode(node, jsonPath) {
    if (!node || typeof node !== 'object') return;
    if (node.type === 'object' && node.additionalProperties !== false) {
      fail('R10-SCHEMA-STRUCTURE', `El subesquema de objeto en ${jsonPath} debe tener "additionalProperties": false.`);
      r6Ok = false;
      r10Ok = false;
    }
    if (node.properties && typeof node.properties === 'object') {
      for (const [k, v] of Object.entries(node.properties)) {
        if (FORBIDDEN_SCHEMA_KEYS.has(k)) {
          fail('R10-SCHEMA-STRUCTURE', `Propiedad prohibida de comando/script/ruta libre encontrada en ${jsonPath}.properties.${k}.`);
          r6Ok = false;
          r10Ok = false;
        }
        inspectNode(v, `${jsonPath}.properties.${k}`);
      }
    }
    if (node.$defs && typeof node.$defs === 'object') {
      for (const [k, v] of Object.entries(node.$defs)) {
        inspectNode(v, `${jsonPath}.$defs.${k}`);
      }
    }
    if (node.items) {
      inspectNode(node.items, `${jsonPath}.items`);
    }
    if (Array.isArray(node.oneOf)) {
      node.oneOf.forEach((branch, idx) => inspectNode(branch, `${jsonPath}.oneOf[${idx}]`));
    }
  }

  inspectNode(schema, 'root');

  // R10: Verificar que changes es un array cuya unión discriminada (oneOf) cubre las 13 primitivas cerradas
  const changesProp = schema.properties?.changes;
  const oneOfList = changesProp?.items?.oneOf;
  if (!changesProp || changesProp.type !== 'array' || !Array.isArray(oneOfList)) {
    fail('R10-SCHEMA-STRUCTURE', 'properties.changes debe ser un array con items.oneOf como unión discriminada.');
    r10Ok = false;
  } else {
    const coveredPrimitives = new Set();
    for (const branch of oneOfList) {
      let resolved = branch;
      if (branch.$ref && branch.$ref.startsWith('#/$defs/')) {
        const defName = branch.$ref.slice('#/$defs/'.length);
        resolved = schema.$defs?.[defName];
      }
      if (!resolved || resolved.type !== 'object' || resolved.additionalProperties !== false) {
        fail('R10-SCHEMA-STRUCTURE', `Rama inválida en changes.items.oneOf (${ JSON.stringify(branch) }).`);
        r10Ok = false;
        continue;
      }
      const constPrim = resolved.properties?.primitive?.const;
      if (typeof constPrim !== 'string') {
        fail('R10-SCHEMA-STRUCTURE', 'Cada rama de changes.items.oneOf debe definir properties.primitive.const.');
        r10Ok = false;
      } else {
        coveredPrimitives.add(constPrim);
      }
    }

    for (const expectedPrim of EXPECTED_ENUMS.primitive) {
      if (!coveredPrimitives.has(expectedPrim)) {
        fail('R10-SCHEMA-STRUCTURE', `La unión discriminada changes.items.oneOf no cubre la primitiva "${expectedPrim}".`);
        r10Ok = false;
      }
    }
    if (coveredPrimitives.size !== EXPECTED_ENUMS.primitive.length) {
      fail('R10-SCHEMA-STRUCTURE', `changes.items.oneOf cubre ${coveredPrimitives.size} primitivas en lugar de ${EXPECTED_ENUMS.primitive.length}.`);
      r10Ok = false;
    }
  }

  if (r6Ok) {
    pass(
      'R6-SCHEMA',
      'schemas/action.schema.json es JSON parseable con la estructura esperada (no es validación formal contra el metaesquema).'
    );
  }
  if (r10Ok) {
    pass(
      'R10-SCHEMA-STRUCTURE',
      'Comprobación estructural de schemas/action.schema.json superada: unión discriminada changes[] para las 13 primitivas, additionalProperties:false y sin campos libres (no es validación formal contra el metaesquema).'
    );
  }

  return schema;
}

// R7. Verificar enlaces relativos en todos los archivos Markdown (normalizando CRLF -> LF y rutas a "/")
function collectMarkdownFiles(dir, list = []) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    if (IGNORED_DIRS.has(entry.name)) continue;
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      collectMarkdownFiles(fullPath, list);
    } else if (entry.isFile() && entry.name.endsWith('.md')) {
      list.push(fullPath);
    }
  }
  return list;
}

function stripFencedCodeBlocks(markdown) {
  return markdown.replace(/```[\s\S]*?```/g, '');
}

function checkMarkdownRelativeLinks() {
  const mdFiles = collectMarkdownFiles(ROOT_DIR);
  const linkRegex = /!?\[[^\]]*\]\(([^)]+)\)/g;
  let brokenCount = 0;
  let checkedLinks = 0;

  for (const mdFile of mdFiles) {
    const raw = readTextNormalized(mdFile);
    const content = stripFencedCodeBlocks(raw);
    let match;
    while ((match = linkRegex.exec(content)) !== null) {
      const rawTarget = match[1].trim().split(/\s+/)[0];
      if (
        !rawTarget ||
        rawTarget.startsWith('#') ||
        rawTarget.startsWith(HTTP_PREFIX) ||
        rawTarget.startsWith(HTTPS_PREFIX) ||
        rawTarget.startsWith('mailto:')
      ) {
        continue;
      }

      const targetPathOnly = rawTarget.split('#')[0];
      if (!targetPathOnly) continue;

      checkedLinks++;
      const resolvedPath = path.resolve(path.dirname(mdFile), targetPathOnly);
      if (!fs.existsSync(resolvedPath)) {
        const relMd = normalizeRelPath(path.relative(ROOT_DIR, mdFile));
        fail('R7-MD-LINKS', `Enlace relativo roto en ${relMd}: "${rawTarget}" no existe.`);
        brokenCount++;
      }
    }
  }

  if (brokenCount === 0) {
    pass('R7-MD-LINKS', `Verificados ${checkedLinks} enlaces relativos en ${mdFiles.length} archivos Markdown sin enlaces rotos.`);
  }
}

// R8. Verificar ausencia de rutas absolutas locales de usuario fuera de ejemplos explícitamente marcados
function checkNoLocalPaths() {
  const allFiles = collectAllRepoFiles(ROOT_DIR);
  const unixLocalPathRegex = new RegExp('(?:^|[^A-Za-z0-9_])(\\/' + 'home\\/[A-Za-z0-9._<>-]+)', 'i');
  const winLocalPathRegex = new RegExp('([A-Za-z]:(?:\\\\\\\\|\\/)?' + 'Users(?:\\\\\\\\|\\/)[A-Za-z0-9._<>-]+)', 'i');
  let findings = 0;

  for (const relPath of allFiles) {
    const ext = path.extname(relPath).toLowerCase();
    if (BINARY_EXTENSIONS.has(ext)) continue;

    const absPath = path.join(ROOT_DIR, relPath);
    const content = readTextNormalized(absPath);
    const lines = content.split('\n');

    for (let idx = 0; idx < lines.length; idx++) {
      const line = lines[idx];
      if (line.includes('EJEMPLO-RUTA-LOCAL')) {
        continue;
      }

      const unixMatch = line.match(unixLocalPathRegex);
      const winMatch = line.match(winLocalPathRegex);
      if (unixMatch || winMatch) {
        const matchedText = (unixMatch && unixMatch[1]) || (winMatch && winMatch[1]);
        fail(
          'R8-NO-LOCAL-PATHS',
          `Ruta absoluta local detectada en ${relPath}:${idx + 1} ("${matchedText}") sin marcador explícito EJEMPLO-RUTA-LOCAL.`
        );
        findings++;
      }
    }
  }

  if (findings === 0) {
    pass('R8-NO-LOCAL-PATHS', `Analizados ${allFiles.length} archivos sin rutas absolutas locales no marcadas.`);
  }
}

// R9. Verificar que ningún archivo de texto versionado contenga URLs http(s) fuera de la allowlist explícita
function checkExternalUrls() {
  const allFiles = collectAllRepoFiles(ROOT_DIR);
  const urlRegex = new RegExp(`(${HTTPS_PREFIX}|${HTTP_PREFIX})[^\\s"'<>\\)\\]]+`, 'g');
  let unauthorizedCount = 0;
  let scannedTextFiles = 0;

  for (const relPath of allFiles) {
    const baseName = path.basename(relPath);
    if (LOCKFILE_BASENAMES.has(baseName)) continue;

    const ext = path.extname(relPath).toLowerCase();
    if (BINARY_EXTENSIONS.has(ext)) continue;

    scannedTextFiles++;
    const absPath = path.join(ROOT_DIR, relPath);
    const content = readTextNormalized(absPath);
    const lines = content.split('\n');

    for (let idx = 0; idx < lines.length; idx++) {
      const line = lines[idx];
      let match;
      urlRegex.lastIndex = 0;
      while ((match = urlRegex.exec(line)) !== null) {
        const foundUrl = match[0].replace(/[.,;]+$/, '');
        if (!ALLOWED_EXTERNAL_URLS.has(foundUrl)) {
          fail(
            'R9-EXTERNAL-URLS',
            `URL externa no autorizada en ${relPath}:${idx + 1} -> "${foundUrl}"`
          );
          unauthorizedCount++;
        }
      }
    }
  }

  if (unauthorizedCount === 0) {
    pass(
      'R9-EXTERNAL-URLS',
      `Analizados ${scannedTextFiles} archivos de texto sin URLs externas fuera de las ${ALLOWED_EXTERNAL_URLS.size} entradas justificadas de la allowlist.`
    );
  }
}

// R11. Comprobación estructural del ejemplo de docs/catalog/CATALOG_SPEC.md frente a schemas/action.schema.json
function matchesPrimitiveType(value, expectedType) {
  if (Array.isArray(expectedType)) {
    return expectedType.some((t) => matchesPrimitiveType(value, t));
  }
  if (expectedType === 'string') return typeof value === 'string';
  if (expectedType === 'integer') return Number.isInteger(value);
  if (expectedType === 'number') return typeof value === 'number' && !Number.isNaN(value);
  if (expectedType === 'boolean') return typeof value === 'boolean';
  if (expectedType === 'null') return value === null;
  if (expectedType === 'array') return Array.isArray(value);
  if (expectedType === 'object') return typeof value === 'object' && value !== null && !Array.isArray(value);
  return false;
}

function validateObjectAgainstSubschema(obj, subschema, rootSchema, label) {
  let ok = true;
  let resolved = subschema;
  if (subschema.$ref && subschema.$ref.startsWith('#/$defs/')) {
    const defName = subschema.$ref.slice('#/$defs/'.length);
    resolved = rootSchema.$defs?.[defName];
  }
  if (!resolved) {
    fail('R11-EXAMPLE-STRUCTURE', `No se pudo resolver subesquema para ${label}.`);
    return false;
  }

  if (!matchesPrimitiveType(obj, 'object')) {
    fail('R11-EXAMPLE-STRUCTURE', `${label} debe ser un objeto.`);
    return false;
  }

  const req = resolved.required || [];
  for (const reqKey of req) {
    if (!(reqKey in obj)) {
      fail('R11-EXAMPLE-STRUCTURE', `Falta propiedad obligatoria "${reqKey}" en ${label}.`);
      ok = false;
    }
  }

  const props = resolved.properties || {};
  if (resolved.additionalProperties === false) {
    for (const actualKey of Object.keys(obj)) {
      if (!(actualKey in props)) {
        fail('R11-EXAMPLE-STRUCTURE', `Propiedad no permitida "${actualKey}" en ${label}.`);
        ok = false;
      }
    }
  }

  for (const [propKey, propSchema] of Object.entries(props)) {
    if (!(propKey in obj)) continue;
    const val = obj[propKey];
    const propLabel = `${label}.${propKey}`;

    if (propSchema.$ref) {
      if (!validateObjectAgainstSubschema(val, propSchema, rootSchema, propLabel)) {
        ok = false;
      }
      continue;
    }

    if (propSchema.type && !matchesPrimitiveType(val, propSchema.type)) {
      fail('R11-EXAMPLE-STRUCTURE', `Tipo inválido en ${propLabel}: se esperaba ${JSON.stringify(propSchema.type)}.`);
      ok = false;
      continue;
    }

    if ('const' in propSchema && val !== propSchema.const) {
      fail('R11-EXAMPLE-STRUCTURE', `Valor const inválido en ${propLabel}: se esperaba "${propSchema.const}".`);
      ok = false;
    }

    if (Array.isArray(propSchema.enum) && !propSchema.enum.includes(val)) {
      fail('R11-EXAMPLE-STRUCTURE', `Valor "${val}" fuera de enum en ${propLabel}.`);
      ok = false;
    }

    if (propSchema.pattern && typeof val === 'string') {
      const re = new RegExp(propSchema.pattern);
      if (!re.test(val)) {
        fail('R11-EXAMPLE-STRUCTURE', `El valor "${val}" en ${propLabel} no cumple el patrón ${propSchema.pattern}.`);
        ok = false;
      }
    }

    if (propSchema.minLength && typeof val === 'string' && val.length < propSchema.minLength) {
      fail('R11-EXAMPLE-STRUCTURE', `El texto en ${propLabel} es menor que minLength (${propSchema.minLength}).`);
      ok = false;
    }

    if (propSchema.type === 'object') {
      if (!validateObjectAgainstSubschema(val, propSchema, rootSchema, propLabel)) {
        ok = false;
      }
    }
  }

  return ok;
}

function checkCatalogSpecExample(schema) {
  if (!schema) return;
  const specPath = path.join(ROOT_DIR, 'docs/catalog/CATALOG_SPEC.md');
  if (!fs.existsSync(specPath)) {
    fail('R11-EXAMPLE-STRUCTURE', 'No existe docs/catalog/CATALOG_SPEC.md.');
    return;
  }

  const content = readTextNormalized(specPath);
  const match = content.match(/```json\n([\s\S]*?)\n```/);
  if (!match) {
    fail('R11-EXAMPLE-STRUCTURE', 'No se encontró bloque de ejemplo ```json en docs/catalog/CATALOG_SPEC.md.');
    return;
  }

  let example;
  try {
    example = JSON.parse(match[1]);
  } catch (err) {
    fail('R11-EXAMPLE-STRUCTURE', `El ejemplo JSON de CATALOG_SPEC.md no es parseable: ${err.message}`);
    return;
  }

  let ok = validateObjectAgainstSubschema(example, schema, schema, 'example');

  if (!Array.isArray(example.changes) || example.changes.length === 0) {
    fail('R11-EXAMPLE-STRUCTURE', 'example.changes debe ser un array no vacío.');
    ok = false;
  } else {
    const oneOfBranches = schema.properties?.changes?.items?.oneOf || [];
    example.changes.forEach((changeItem, idx) => {
      const prim = changeItem?.primitive;
      const matchingBranch = oneOfBranches.find((b) => {
        const defName = b.$ref?.slice('#/$defs/'.length);
        const resolved = schema.$defs?.[defName];
        return resolved?.properties?.primitive?.const === prim;
      });
      if (!matchingBranch) {
        fail('R11-EXAMPLE-STRUCTURE', `example.changes[${idx}] tiene primitive "${prim}" que no coincide con ninguna rama de oneOf.`);
        ok = false;
      } else if (!validateObjectAgainstSubschema(changeItem, matchingBranch, schema, `example.changes[${idx}]`)) {
        ok = false;
      }
    });
  }

  if (ok) {
    pass(
      'R11-EXAMPLE-STRUCTURE',
      'Comprobación estructural del ejemplo de CATALOG_SPEC.md superada: campos requeridos, tipos, enums, patrones y rama de changes[] verificados.'
    );
  }
}

// R12. Validar manifiesto de módulos (apps/desktop/src/modules/modules.manifest.json)
function checkModulesManifest() {
  const schemaPath = path.join(ROOT_DIR, 'schemas/modules-manifest.schema.json');
  const manifestPath = path.join(ROOT_DIR, 'apps/desktop/src/modules/modules.manifest.json');
  if (!fs.existsSync(schemaPath)) {
    fail('R12-MODULES-MANIFEST', 'No existe schemas/modules-manifest.schema.json.');
    return;
  }
  if (!fs.existsSync(manifestPath)) {
    fail('R12-MODULES-MANIFEST', 'No existe apps/desktop/src/modules/modules.manifest.json.');
    return;
  }

  let schema;
  let manifest;
  try {
    schema = JSON.parse(readTextNormalized(schemaPath));
  } catch (err) {
    fail('R12-MODULES-MANIFEST', `schemas/modules-manifest.schema.json no es JSON parseable: ${err.message}`);
    return;
  }
  try {
    manifest = JSON.parse(readTextNormalized(manifestPath));
  } catch (err) {
    fail('R12-MODULES-MANIFEST', `modules.manifest.json no es JSON parseable: ${err.message}`);
    return;
  }

  let ok = true;
  const expectedOrder = schema.properties?.modules?.items?.properties?.id?.enum || [
    'gaming', 'memory', 'system', 'network', 'storage', 'security'
  ];
  const allowedCaps = schema.properties?.modules?.items?.properties?.capabilities?.items?.enum || [
    'probes', 'dossiers', 'actions'
  ];

  const rootAllowed = new Set(Object.keys(schema.properties || {}));
  for (const k of Object.keys(manifest)) {
    if (!rootAllowed.has(k)) {
      fail('R12-MODULES-MANIFEST', `Propiedad extra no permitida en raíz del manifiesto: "${k}".`);
      ok = false;
    }
  }
  if (manifest.schemaVersion !== (schema.properties?.schemaVersion?.const ?? 1)) {
    fail('R12-MODULES-MANIFEST', `schemaVersion inválido: se esperaba 1, recibido ${manifest.schemaVersion}.`);
    ok = false;
  }
  if (!Array.isArray(manifest.modules) || manifest.modules.length !== expectedOrder.length) {
    fail('R12-MODULES-MANIFEST', `modules debe tener exactamente ${expectedOrder.length} elementos.`);
    ok = false;
  } else {
    const seenIds = new Set();
    const seenCats = new Set();
    const itemAllowed = new Set(Object.keys(schema.properties?.modules?.items?.properties || {}));

    manifest.modules.forEach((mod, idx) => {
      if (!mod || typeof mod !== 'object' || Array.isArray(mod)) {
        fail('R12-MODULES-MANIFEST', `modules[${idx}] debe ser un objeto.`);
        ok = false;
        return;
      }
      for (const k of Object.keys(mod)) {
        if (!itemAllowed.has(k)) {
          fail('R12-MODULES-MANIFEST', `Propiedad extra no permitida "${k}" en modules[${idx}].`);
          ok = false;
        }
      }
      if (seenIds.has(mod.id)) {
        fail('R12-MODULES-MANIFEST', `id de módulo duplicado: "${mod.id}".`);
        ok = false;
      }
      seenIds.add(mod.id);
      if (mod.id !== expectedOrder[idx]) {
        fail('R12-MODULES-MANIFEST', `Orden o id inválido en modules[${idx}]: se esperaba "${expectedOrder[idx]}", recibido "${mod.id}".`);
        ok = false;
      }
      if (!Array.isArray(mod.categories) || mod.categories.length === 0) {
        fail('R12-MODULES-MANIFEST', `modules[${idx}].categories debe ser un array no vacío.`);
        ok = false;
      } else {
        for (const cat of mod.categories) {
          if (!Number.isInteger(cat) || cat < 1 || cat > 17) {
            fail('R12-MODULES-MANIFEST', `Categoría inválida ${cat} en ${mod.id} (solo se permiten enteros 1..17).`);
            ok = false;
          } else if (seenCats.has(cat)) {
            fail('R12-MODULES-MANIFEST', `Categoría repetida ${cat} en ${mod.id}.`);
            ok = false;
          } else {
            seenCats.add(cat);
          }
        }
      }
      if (!Array.isArray(mod.capabilities) || mod.capabilities.length !== allowedCaps.length) {
        fail('R12-MODULES-MANIFEST', `modules[${idx}].capabilities debe contener exactamente [${allowedCaps.join(', ')}].`);
        ok = false;
      } else {
        for (const cap of mod.capabilities) {
          if (!allowedCaps.includes(cap)) {
            fail('R12-MODULES-MANIFEST', `Capability desconocida "${cap}" en ${mod.id}.`);
            ok = false;
          }
        }
      }
    });

    for (let c = 1; c <= 17; c++) {
      if (!seenCats.has(c)) {
        fail('R12-MODULES-MANIFEST', `Falta la categoría ${c} en el reparto de módulos.`);
        ok = false;
      }
    }
  }

  if (ok) {
    pass('R12-MODULES-MANIFEST', 'Manifiesto de módulos válido: 6 módulos en orden, categorías 1..17 cubiertas una vez, sin categoría 18 ni campos extra.');
  }
}

// R13. Validar contrato IPC de solo lectura (schemas/ipc-host.schema.json)
function checkIpcHostSchema() {
  const schemaPath = path.join(ROOT_DIR, 'schemas/ipc-host.schema.json');
  if (!fs.existsSync(schemaPath)) {
    fail('R13-IPC-CONTRACT', 'No existe schemas/ipc-host.schema.json.');
    return;
  }

  let schema;
  try {
    schema = JSON.parse(readTextNormalized(schemaPath));
  } catch (err) {
    fail('R13-IPC-CONTRACT', `schemas/ipc-host.schema.json no es JSON parseable: ${err.message}`);
    return;
  }

  let ok = true;
  const expectedMethods = ['engine.hello', 'engine.version', 'system.osInfo'];
  const expectedMethodSet = new Set(expectedMethods);

  function inspectObj(node, jsonPath) {
    if (!node || typeof node !== 'object') return;
    if (node.type === 'object' && node.additionalProperties !== false) {
      fail('R13-IPC-CONTRACT', `El subesquema de objeto en ${jsonPath} debe tener "additionalProperties": false.`);
      ok = false;
    }
    if (Array.isArray(node)) {
      node.forEach((item, idx) => inspectObj(item, `${jsonPath}[${idx}]`));
    } else {
      for (const [k, v] of Object.entries(node)) {
        if (v && typeof v === 'object') inspectObj(v, `${jsonPath}.${k}`);
      }
    }
  }
  inspectObj(schema, 'root');

  const oneOf = schema.properties?.methods?.items?.oneOf;
  if (!Array.isArray(oneOf) || oneOf.length !== expectedMethods.length) {
    fail('R13-IPC-CONTRACT', `methods.items.oneOf debe tener exactamente ${expectedMethods.length} métodos.`);
    ok = false;
  } else {
    const actualMethods = [];
    for (const branch of oneOf) {
      const ref = branch.$ref;
      const defName = typeof ref === 'string' && ref.startsWith('#/$defs/') ? ref.slice('#/$defs/'.length) : null;
      const resolved = defName ? schema.$defs?.[defName] : branch;
      const mName = resolved?.properties?.name?.const;
      if (typeof mName !== 'string') {
        fail('R13-IPC-CONTRACT', 'Cada rama de methods.items.oneOf debe definir properties.name.const.');
        ok = false;
      } else {
        actualMethods.push(mName);
        if (!expectedMethodSet.has(mName)) {
          fail('R13-IPC-CONTRACT', `Método fuera de la lista permitida: "${mName}".`);
          ok = false;
        }
      }
    }
    if (actualMethods.length === expectedMethods.length) {
      for (let i = 0; i < expectedMethods.length; i++) {
        if (actualMethods[i] !== expectedMethods[i]) {
          fail('R13-IPC-CONTRACT', `Orden o método inválido en posición ${i}: se esperaba "${expectedMethods[i]}", recibido "${actualMethods[i]}".`);
          ok = false;
        }
      }
    }
  }

  const helloEnum = schema.$defs?.methodEngineHello?.properties?.result?.properties?.methods?.items?.enum;
  if (Array.isArray(helloEnum)) {
    for (const m of helloEnum) {
      if (!expectedMethodSet.has(m)) {
        fail('R13-IPC-CONTRACT', `Método fuera de la lista permitida en engine.hello.result.methods: "${m}".`);
        ok = false;
      }
    }
  }

  if (ok) {
    pass('R13-IPC-CONTRACT', 'Contrato IPC schemas/ipc-host.schema.json válido: 3 métodos en orden y additionalProperties:false.');
  }
}

checkNoLicenseFile();
checkStructureAndPackageJson();
checkAdrs();
const parsedSchema = checkActionSchema();
checkMarkdownRelativeLinks();
checkNoLocalPaths();
checkExternalUrls();
checkCatalogSpecExample(parsedSchema);
checkModulesManifest();
checkIpcHostSchema();

if (errors.length > 0) {
  console.error(`\nResultado: FALLÓ con ${errors.length} error(es).`);
  process.exit(1);
} else {
  console.log('\nResultado: TODAS LAS COMPROBACIONES (R1-R13) SUPERADAS (código 0).');
  process.exit(0);
}
