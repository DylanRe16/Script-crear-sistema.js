'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const test = require('node:test');

const {
  actualizarContenidoEnv,
  actualizarProxyConfig,
  copiarCarpeta,
  crearCadenaConexion,
  debeIgnorar,
  normalizarApiTarget,
  normalizarNombreSistema,
  normalizarRutaBase,
  validarIdentificadorPostgres
} = require('./crear-sistema');

test('normaliza nombres de sistema y rechaza nombres inseguros o demasiado largos', () => {
  assert.equal(normalizarNombreSistema('  Sistema Ventas  '), 'sistema-ventas');
  assert.throws(() => normalizarNombreSistema('../mi-sistema'));
  assert.throws(() => normalizarNombreSistema('a'.repeat(55)));
  assert.throws(() => normalizarNombreSistema('con'));
});

test('valida PathBase, host de API e identificadores PostgreSQL', () => {
  assert.equal(normalizarRutaBase('/VentasBackend/api/'), '/VentasBackend/api');
  assert.throws(() => normalizarRutaBase('VentasBackend/api'));
  assert.equal(normalizarApiTarget('http://localhost:5136/'), 'http://localhost:5136');
  assert.throws(() => normalizarApiTarget('http://localhost:5136/api'));
  assert.equal(validarIdentificadorPostgres('ventas_db', 'Base'), 'ventas_db');
  assert.throws(() => validarIdentificadorPostgres('ventas-db', 'Base'));
});

test('configura proxy /api y mantiene el script start con el archivo JSON', () => {
  const directorio = fs.mkdtempSync(path.join(os.tmpdir(), 'crear-sistema-proxy-'));
  try {
    fs.writeFileSync(
      path.join(directorio, 'package.json'),
      JSON.stringify({ scripts: { start: 'ng serve --proxy-config proxy.conf.json' } })
    );
    fs.writeFileSync(
      path.join(directorio, 'proxy.conf.json'),
      JSON.stringify({ '/status': { target: 'http://localhost:1' } })
    );

    actualizarProxyConfig(directorio, 'http://localhost:5136', '/VentasBackend/api');

    const config = JSON.parse(fs.readFileSync(path.join(directorio, 'proxy.conf.json'), 'utf8'));
    const paquete = JSON.parse(fs.readFileSync(path.join(directorio, 'package.json'), 'utf8'));
    assert.deepEqual(config['/api'], {
      target: 'http://localhost:5136',
      secure: false,
      changeOrigin: true,
      pathRewrite: { '^/api': '/VentasBackend/api' },
      logLevel: 'debug'
    });
    assert.equal(config['/status'].target, 'http://localhost:1');
    assert.equal(paquete.scripts.start, 'ng serve --proxy-config proxy.conf.json');
  } finally {
    fs.rmSync(directorio, { recursive: true, force: true });
  }
});

test('migra la configuración proxy de package.json al archivo que usa ng serve', () => {
  const directorio = fs.mkdtempSync(path.join(os.tmpdir(), 'crear-sistema-proxy-package-'));
  try {
    fs.writeFileSync(
      path.join(directorio, 'package.json'),
      JSON.stringify({
        scripts: { start: 'ng serve' },
        proxy: { '/api': { target: 'http://localhost:5000' } }
      })
    );

    actualizarProxyConfig(directorio, 'http://localhost:5136', '/VentasBackend/api');

    const paquete = JSON.parse(fs.readFileSync(path.join(directorio, 'package.json'), 'utf8'));
    const config = JSON.parse(fs.readFileSync(path.join(directorio, 'proxy.conf.json'), 'utf8'));
    assert.equal(Object.prototype.hasOwnProperty.call(paquete, 'proxy'), false);
    assert.match(paquete.scripts.start, /--proxy-config proxy\.conf\.json/);
    assert.equal(config['/api'].pathRewrite['^/api'], '/VentasBackend/api');
  } finally {
    fs.rmSync(directorio, { recursive: true, force: true });
  }
});

test('actualiza variables .env conservando comentarios y eliminando valores duplicados', () => {
  const resultado = actualizarContenidoEnv(
    '# configuración local\nPathBase=/antiguo\nPathBase=/duplicado\nOTRA=conservar\n',
    { PathBase: '/nuevo/api' }
  );
  assert.equal(resultado, '# configuración local\nPathBase=/nuevo/api\nOTRA=conservar\n');
});

test('escapa valores especiales de la cadena PostgreSQL y excluye secretos y artefactos', () => {
  const cadena = crearCadenaConexion({
    dbHost: 'localhost',
    dbPort: '5432',
    nombreBd: 'ventas_db',
    dbUser: 'usuario',
    dbPass: 'p;ass word'
  });
  assert.match(cadena, /Password="p;ass word"$/);
  assert.equal(debeIgnorar('.env', false), true);
  assert.equal(debeIgnorar('.env.example', false), false);
  assert.equal(debeIgnorar('local.db-wal', false), true);
  assert.equal(debeIgnorar('bin', true), true);
});

test('copia plantillas sin transferir .env, bases de datos ni artefactos compilados', () => {
  const directorio = fs.mkdtempSync(path.join(os.tmpdir(), 'crear-sistema-copy-'));
  const origen = path.join(directorio, 'origen');
  const destino = path.join(directorio, 'destino');
  try {
    fs.mkdirSync(path.join(origen, 'bin'), { recursive: true });
    fs.writeFileSync(path.join(origen, '.env'), 'SECRET=not-copied');
    fs.writeFileSync(path.join(origen, '.env.example'), 'SETTING=example');
    fs.writeFileSync(path.join(origen, 'local.db'), 'database');
    fs.writeFileSync(path.join(origen, 'keep.json'), '{}');
    fs.writeFileSync(path.join(origen, 'bin', 'compiled.dll'), 'artifact');

    copiarCarpeta(origen, destino);

    assert.equal(fs.existsSync(path.join(destino, '.env')), false);
    assert.equal(fs.existsSync(path.join(destino, 'local.db')), false);
    assert.equal(fs.existsSync(path.join(destino, 'bin')), false);
    assert.equal(fs.existsSync(path.join(destino, '.env.example')), true);
    assert.equal(fs.existsSync(path.join(destino, 'keep.json')), true);
  } finally {
    fs.rmSync(directorio, { recursive: true, force: true });
  }
});
