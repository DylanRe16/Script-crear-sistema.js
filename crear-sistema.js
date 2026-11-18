#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { execFileSync, execSync } = require('child_process');
const readline = require('readline');
const crypto = require('crypto');

const RUTA_PLANTILLA_FRONT = path.resolve(
  process.env.PLANTILLA_FRONT || path.join(__dirname, '_plantilla-base')
);
const RUTA_PLANTILLA_BACK = path.resolve(
  process.env.PLANTILLA_BACK || path.join(__dirname, '_plantilla-backend')
);
const RUTA_DESTINO_BASE = path.resolve(
  process.env.SISTEMAS_DESTINO || path.join(__dirname, 'sistemas')
);

const DIRECTORIOS_IGNORADOS = new Set([
  '.angular',
  '.git',
  '.idea',
  '.vs',
  'bin',
  'coverage',
  'dist',
  'node_modules',
  'obj',
  'publish',
  'TestResults'
]);

function esObjeto(valor) {
  return valor !== null && typeof valor === 'object' && !Array.isArray(valor);
}

function normalizarNombreSistema(valor) {
  const nombre = String(valor || '')
    .trim()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .replace(/[\s_]+/g, '-');

  if (!/^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$/.test(nombre) || nombre.length > 54) {
    throw new Error('Usa un nombre tipo slug: letras minúsculas, números y guiones; debe empezar con una letra.');
  }

  if (/^(con|prn|aux|nul|com[1-9]|lpt[1-9])$/.test(nombre)) {
    throw new Error(`"${nombre}" es un nombre reservado por Windows.`);
  }

  return nombre;
}

function validarIdentificadorPostgres(valor, etiqueta) {
  const identificador = String(valor || '').trim();
  if (!/^[A-Za-z_][A-Za-z0-9_]{0,62}$/.test(identificador)) {
    throw new Error(`${etiqueta} debe iniciar con letra o guion bajo y contener solo letras, números y guion bajo (máximo 63 caracteres).`);
  }
  return identificador;
}

function normalizarRutaBase(valor) {
  const ruta = String(valor || '').trim().replace(/\/+$/, '');
  if (
    !ruta.startsWith('/') ||
    ruta === '/' ||
    !/^\/[A-Za-z0-9_-]+(?:\/[A-Za-z0-9_-]+)*$/.test(ruta)
  ) {
    throw new Error('La ruta base debe comenzar con / y usar segmentos con letras, números, guiones o guiones bajos.');
  }
  return ruta;
}

function normalizarApiTarget(valor) {
  let url;
  try {
    url = new URL(String(valor || '').trim());
  } catch {
    throw new Error('El host del backend debe ser una URL HTTP o HTTPS válida.');
  }

  if (
    !['http:', 'https:'].includes(url.protocol) ||
    !url.hostname ||
    url.username ||
    url.password ||
    (url.pathname !== '/' && url.pathname !== '') ||
    url.search ||
    url.hash
  ) {
    throw new Error('El host debe ser un origen, por ejemplo http://localhost:5136, sin ruta ni credenciales.');
  }

  return url.origin;
}

function estaDentroDe(carpeta, ruta) {
  const relativo = path.relative(carpeta, ruta);
  return relativo === '' || (!relativo.startsWith(`..${path.sep}`) && relativo !== '..' && !path.isAbsolute(relativo));
}

function debeIgnorar(item, esDirectorio) {
  if (esDirectorio && DIRECTORIOS_IGNORADOS.has(item)) return true;
  if (item === '.env' || (item.startsWith('.env.') && item !== '.env.example')) return true;
  if (/\.db(?:-(?:shm|wal))?$/i.test(item)) return true;
  if (/\.sqlite(?:3)?(?:-(?:shm|wal))?$/i.test(item)) return true;
  return false;
}

function copiarCarpeta(origen, destino, ignorar = []) {
  if (!fs.existsSync(origen) || !fs.statSync(origen).isDirectory()) {
    throw new Error(`No existe la carpeta de plantilla: ${origen}`);
  }

  fs.mkdirSync(destino, { recursive: true });

  for (const entrada of fs.readdirSync(origen, { withFileTypes: true })) {
    const rutaOrigen = path.join(origen, entrada.name);
    const rutaDestino = path.join(destino, entrada.name);
    if (ignorar.includes(entrada.name) || debeIgnorar(entrada.name, entrada.isDirectory())) continue;
    if (entrada.isSymbolicLink()) continue;

    if (entrada.isDirectory()) {
      copiarCarpeta(rutaOrigen, rutaDestino, ignorar);
    } else if (entrada.isFile()) {
      fs.copyFileSync(rutaOrigen, rutaDestino);
    }
  }
}

function leerJson(ruta) {
  try {
    return JSON.parse(fs.readFileSync(ruta, 'utf8'));
  } catch (error) {
    throw new Error(`No se pudo leer JSON válido en ${ruta}: ${error.message}`);
  }
}

function escribirJson(ruta, contenido) {
  fs.writeFileSync(ruta, `${JSON.stringify(contenido, null, 2)}\n`, 'utf8');
}

function actualizarProxyConfig(rutaFront, apiTarget, rutaBase) {
  const paquetePath = path.join(rutaFront, 'package.json');
  const paquete = leerJson(paquetePath);
  const candidatos = ['proxy.conf.json', 'proxy.config.json'];
  let proxyNombre = candidatos.find((nombre) => fs.existsSync(path.join(rutaFront, nombre)));
  let proxyPath;
  let proxyConfig;
  let migrarProxyDePaquete = false;

  if (proxyNombre) {
    proxyPath = path.join(rutaFront, proxyNombre);
    proxyConfig = leerJson(proxyPath);
  } else if (esObjeto(paquete.proxy)) {
    proxyNombre = 'proxy.conf.json';
    proxyPath = path.join(rutaFront, proxyNombre);
    proxyConfig = paquete.proxy;
    migrarProxyDePaquete = true;
  } else {
    proxyNombre = 'proxy.conf.json';
    proxyPath = path.join(rutaFront, proxyNombre);
    proxyConfig = {};
  }

  if (!esObjeto(proxyConfig)) {
    throw new Error(`La configuración del proxy debe ser un objeto JSON: ${proxyPath}`);
  }

  proxyConfig['/api'] = {
    ...(esObjeto(proxyConfig['/api']) ? proxyConfig['/api'] : {}),
    target: apiTarget,
    secure: false,
    changeOrigin: true,
    pathRewrite: { '^/api': rutaBase },
    logLevel: 'debug'
  };
  escribirJson(proxyPath, proxyConfig);

  paquete.scripts = esObjeto(paquete.scripts) ? paquete.scripts : {};
  let scriptStart = paquete.scripts.start || 'ng serve';
  const proxyArgumento = /--proxy-config(?:=|\s+)(?:"[^"]+"|'[^']+'|[^\s]+)/;
  if (proxyArgumento.test(scriptStart)) {
    scriptStart = scriptStart.replace(proxyArgumento, `--proxy-config ${proxyNombre}`);
  } else {
    scriptStart = `${scriptStart} --proxy-config ${proxyNombre}`;
  }
  paquete.scripts.start = scriptStart;
  if (migrarProxyDePaquete) delete paquete.proxy;
  escribirJson(paquetePath, paquete);

  return proxyNombre;
}

function actualizarPaqueteFrontend(rutaFront, nombre) {
  const packagePath = path.join(rutaFront, 'package.json');
  const paquete = leerJson(packagePath);
  paquete.name = nombre;
  escribirJson(packagePath, paquete);

  const lockPath = path.join(rutaFront, 'package-lock.json');
  if (fs.existsSync(lockPath)) {
    const lock = leerJson(lockPath);
    lock.name = nombre;
    if (esObjeto(lock.packages) && esObjeto(lock.packages[''])) {
      lock.packages[''].name = nombre;
    }
    escribirJson(lockPath, lock);
  }
}

function reemplazarEnArchivo(ruta, buscar, reemplazar) {
  if (!fs.existsSync(ruta)) return false;
  const original = fs.readFileSync(ruta, 'utf8');
  const actualizado = original.split(buscar).join(reemplazar);
  if (actualizado === original) return false;
  fs.writeFileSync(ruta, actualizado, 'utf8');
  return true;
}

function actualizarAngularJson(rutaFront, nombre) {
  const angularPath = path.join(rutaFront, 'angular.json');
  const angular = leerJson(angularPath);
  if (!esObjeto(angular.projects) || Object.keys(angular.projects).length === 0) {
    throw new Error('angular.json no declara ningún proyecto.');
  }

  const nombreAnterior = Object.keys(angular.projects)[0];
  const proyecto = angular.projects[nombreAnterior];
  if (nombreAnterior !== nombre) {
    delete angular.projects[nombreAnterior];
    angular.projects[nombre] = proyecto;
  }

  function actualizarReferencias(nodo) {
    if (Array.isArray(nodo)) {
      nodo.forEach(actualizarReferencias);
      return;
    }
    if (!esObjeto(nodo)) return;

    for (const [clave, valor] of Object.entries(nodo)) {
      if (clave === 'buildTarget' && typeof valor === 'string') {
        nodo[clave] = valor.replace(new RegExp(`^${nombreAnterior}:`), `${nombre}:`);
      } else if (clave === 'outputPath' && typeof valor === 'string' && valor.startsWith('dist/')) {
        nodo[clave] = `dist/${nombre}`;
      } else if (clave === 'path' && typeof valor === 'string' && valor.startsWith('dist/')) {
        nodo[clave] = `dist/${nombre}`;
      } else {
        actualizarReferencias(valor);
      }
    }
  }

  actualizarReferencias(proyecto);
  if (angular.defaultProject === nombreAnterior) angular.defaultProject = nombre;
  escribirJson(angularPath, angular);
}

function reemplazarPrefijoAlmacenamiento(rutaBase, prefijoAnterior, prefijoNuevo) {
  let cambios = 0;

  function recorrer(directorio) {
    for (const entrada of fs.readdirSync(directorio, { withFileTypes: true })) {
      if (debeIgnorar(entrada.name, entrada.isDirectory()) || entrada.isSymbolicLink()) continue;
      const ruta = path.join(directorio, entrada.name);
      if (entrada.isDirectory()) {
        recorrer(ruta);
      } else if (/\.(?:ts|html|js|mjs)$/i.test(entrada.name)) {
        const original = fs.readFileSync(ruta, 'utf8');
        const actualizado = original.split(prefijoAnterior).join(prefijoNuevo);
        if (actualizado !== original) {
          fs.writeFileSync(ruta, actualizado, 'utf8');
          cambios++;
        }
      }
    }
  }

  recorrer(rutaBase);
  return cambios;
}

function encontrarProyectosDotnet(dir) {
  const proyectos = [];

  function recorrer(directorio) {
    for (const entrada of fs.readdirSync(directorio, { withFileTypes: true })) {
      if (debeIgnorar(entrada.name, entrada.isDirectory()) || entrada.isSymbolicLink()) continue;
      const ruta = path.join(directorio, entrada.name);
      if (entrada.isDirectory()) {
        recorrer(ruta);
      } else if (entrada.name.endsWith('.csproj')) {
        proyectos.push(ruta);
      }
    }
  }

  recorrer(dir);
  return proyectos;
}

function actualizarProyectoDotnet(rutaProyecto, nombre, esquema) {
  let contenido = fs.readFileSync(rutaProyecto, 'utf8');

  function actualizarElementoXml(tag, valor) {
    const regex = new RegExp(`<${tag}>[\\s\\S]*?</${tag}>`);
    const nuevoElemento = `<${tag}>${valor}</${tag}>`;
    if (regex.test(contenido)) {
      contenido = contenido.replace(regex, nuevoElemento);
    } else {
      contenido = contenido.replace(
        /<PropertyGroup>/,
        `<PropertyGroup>\n    ${nuevoElemento}`
      );
    }
  }

  actualizarElementoXml('AssemblyName', `${nombre}.Api`);
  actualizarElementoXml('RootNamespace', esquema);
  fs.writeFileSync(rutaProyecto, contenido, 'utf8');

  const proyectoNuevo = path.join(path.dirname(rutaProyecto), `${nombre}.Api.csproj`);
  if (path.resolve(proyectoNuevo) !== path.resolve(rutaProyecto)) {
    if (fs.existsSync(proyectoNuevo)) {
      throw new Error(`Ya existe el proyecto de destino: ${proyectoNuevo}`);
    }
    fs.renameSync(rutaProyecto, proyectoNuevo);
  }
  return proyectoNuevo;
}

function reemplazarEnCarpeta(rutaBase, buscar, reemplazar, extensiones = ['.cs', '.sql', '.json', '.md', '.http']) {
  let cambios = 0;

  function recorrer(directorio) {
    for (const entrada of fs.readdirSync(directorio, { withFileTypes: true })) {
      if (debeIgnorar(entrada.name, entrada.isDirectory()) || entrada.isSymbolicLink()) continue;
      const ruta = path.join(directorio, entrada.name);
      if (entrada.isDirectory()) {
        recorrer(ruta);
      } else if (extensiones.includes(path.extname(entrada.name).toLowerCase())) {
        const original = fs.readFileSync(ruta, 'utf8');
        const actualizado = original.split(buscar).join(reemplazar);
        if (actualizado !== original) {
          fs.writeFileSync(ruta, actualizado, 'utf8');
          cambios++;
        }
      }
    }
  }

  recorrer(rutaBase);
  return cambios;
}

function escaparValorConnectionString(valor) {
  const texto = String(valor);
  if (!/[;\s'"\\]/.test(texto)) return texto;
  return `"${texto.replace(/"/g, '""')}"`;
}

function crearCadenaConexion({ dbHost, dbPort, nombreBd, dbUser, dbPass }) {
  return [
    `Host=${escaparValorConnectionString(dbHost)}`,
    `Port=${dbPort}`,
    `Database=${escaparValorConnectionString(nombreBd)}`,
    `Username=${escaparValorConnectionString(dbUser)}`,
    `Password=${escaparValorConnectionString(dbPass)}`
  ].join(';');
}

function actualizarContenidoEnv(contenido, variables) {
  const lineas = String(contenido || '').split(/\r?\n/);
  const clavesActualizadas = new Set();
  const resultado = [];

  for (const linea of lineas) {
    const coincidencia = linea.match(/^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=/);
    const clave = coincidencia?.[1];
    if (clave && Object.prototype.hasOwnProperty.call(variables, clave)) {
      if (!clavesActualizadas.has(clave)) {
        resultado.push(`${clave}=${variables[clave]}`);
        clavesActualizadas.add(clave);
      }
    } else if (linea !== '' || resultado.length > 0) {
      resultado.push(linea);
    }
  }

  for (const [clave, valor] of Object.entries(variables)) {
    if (!clavesActualizadas.has(clave)) resultado.push(`${clave}=${valor}`);
  }

  return `${resultado.join('\n').replace(/\n+$/, '')}\n`;
}

function actualizarEnv(rutaProyecto, configuracion) {
  const envPath = path.join(rutaProyecto, '.env');
  const examplePath = path.join(rutaProyecto, '.env.example');
  const contenidoBase = fs.existsSync(envPath)
    ? fs.readFileSync(envPath, 'utf8')
    : fs.existsSync(examplePath)
      ? fs.readFileSync(examplePath, 'utf8')
      : '';

  const cadenaConexion = crearCadenaConexion(configuracion);
  const variables = {
    ConnectionStrings__DefaultConnection: cadenaConexion,
    POSTGRESQLCONNSTR_DefaultConnection: cadenaConexion,
    JwtSettings__SecretKey: configuracion.jwtKey,
    SecuritySettings__DefaultResetPassword: configuracion.passwordTemporal,
    PathBase: configuracion.rutaBase,
    DB_HOST: configuracion.dbHost,
    DB_PORT: configuracion.dbPort,
    DB_NAME: configuracion.nombreBd,
    DB_USER: configuracion.dbUser,
    DB_PASSWORD: configuracion.dbPass
  };

  fs.writeFileSync(envPath, actualizarContenidoEnv(contenidoBase, variables), 'utf8');
}

function actualizarPathBaseAppsettings(rutaProyecto, rutaBase) {
  const archivos = ['appsettings.json', 'appsettings.Development.json'];
  for (const nombre of archivos) {
    const ruta = path.join(rutaProyecto, nombre);
    if (!fs.existsSync(ruta)) continue;
    const configuracion = leerJson(ruta);
    if (esObjeto(configuracion.ConnectionStrings)) {
      delete configuracion.ConnectionStrings.DefaultConnection;
      delete configuracion.ConnectionStrings.PostgresConnection;
    }
    if (esObjeto(configuracion.JwtSettings)) {
      delete configuracion.JwtSettings.SecretKey;
    }
    configuracion.PathBase = rutaBase;
    escribirJson(ruta, configuracion);
  }
}

function generarClaveJwt() {
  return crypto.randomBytes(48).toString('base64').replace(/[^a-zA-Z0-9]/g, '').slice(0, 64);
}

function generarPasswordTemporal() {
  let password;
  do {
    password = crypto.randomBytes(48).toString('base64').replace(/[^a-zA-Z0-9]/g, '').slice(0, 40);
  } while (!/[a-z]/.test(password) || !/[A-Z]/.test(password) || !/\d/.test(password));
  return password;
}

function preguntar(rl, texto) {
  return new Promise((resolve, reject) => {
    if (rl.closed) {
      reject(new Error('La entrada terminó antes de responder todas las preguntas.'));
      return;
    }

    const alCerrar = () => reject(new Error('La entrada terminó antes de responder todas las preguntas.'));
    rl.once('close', alCerrar);
    try {
      rl.question(texto, (respuesta) => {
        rl.removeListener('close', alCerrar);
        resolve(respuesta);
      });
    } catch (error) {
      rl.removeListener('close', alCerrar);
      reject(error);
    }
  });
}

function preguntarSecreto(rl, texto) {
  const entrada = process.stdin;
  if (!entrada.isTTY || typeof entrada.setRawMode !== 'function') {
    return preguntar(rl, texto).then((valor) => ({ valor, rl }));
  }

  rl.close();
  readline.emitKeypressEvents(entrada);
  entrada.setRawMode(true);
  entrada.resume();
  process.stdout.write(texto);

  return new Promise((resolve) => {
    let valor = '';
    let terminado = false;

    function finalizar(cancelado = false) {
      if (terminado) return;
      terminado = true;
      entrada.removeListener('keypress', alPresionarTecla);
      entrada.setRawMode(false);
      process.stdout.write('\n');
      const nuevaInterfaz = readline.createInterface({ input: entrada, output: process.stdout });
      resolve({ valor, rl: nuevaInterfaz, cancelado });
    }

    function alPresionarTecla(caracter, tecla = {}) {
      if (tecla.ctrl && ['c', 'd'].includes(tecla.name)) {
        finalizar(true);
      } else if (tecla.name === 'return' || tecla.name === 'enter') {
        finalizar();
      } else if (tecla.name === 'backspace') {
        if (valor.length > 0) {
          valor = valor.slice(0, -1);
          process.stdout.write('\b \b');
        }
      } else if (!tecla.ctrl && !tecla.meta && caracter) {
        valor += caracter;
        process.stdout.write('*');
      }
    }

    entrada.on('keypress', alPresionarTecla);
  });
}

async function preguntarSiNo(rl, texto, porDefecto = true) {
  while (true) {
    const respuesta = (await preguntar(rl, texto)).trim().toLowerCase();
    if (!respuesta) return porDefecto;
    if (['s', 'si', 'sí', 'y', 'yes'].includes(respuesta)) return true;
    if (['n', 'no'].includes(respuesta)) return false;
    console.log('Responde "s" o "n".');
  }
}

function exigirArchivo(ruta) {
  if (!fs.existsSync(ruta) || !fs.statSync(ruta).isFile()) {
    throw new Error(`Falta un archivo requerido de la plantilla: ${ruta}`);
  }
}

function ejecutarDotnet(args, cwd) {
  execFileSync('dotnet', args, { cwd, stdio: 'inherit' });
}

function ejecutarNpm(args, cwd) {
  execSync(`npm ${args.join(' ')}`, { cwd, stdio: 'inherit' });
}

async function main() {
  let rl = readline.createInterface({ input: process.stdin, output: process.stdout });

  try {
    console.log('\nCreador de sistemas: Angular + .NET + PostgreSQL\n');
    exigirArchivo(path.join(RUTA_PLANTILLA_FRONT, 'package.json'));
    exigirArchivo(path.join(RUTA_PLANTILLA_FRONT, 'angular.json'));
    exigirArchivo(path.join(RUTA_PLANTILLA_FRONT, 'src', 'app', 'app.routes.ts'));

    const nombre = normalizarNombreSistema(
      await preguntar(rl, 'Nombre del nuevo sistema (slug; ej: sistema-ventas): ')
    );
    const generarBackend = await preguntarSiNo(rl, '¿Generar backend .NET? (s/n) [s]: ');
    const apiTarget = normalizarApiTarget(
      (await preguntar(rl, 'Host del backend [http://localhost:5136]: ')).trim() || 'http://localhost:5136'
    );

    const identificadorBase = nombre.replace(/-/g, '_');
    let esquema = identificadorBase;
    let nombreBd = `${identificadorBase}_db`;
    let dbHost = 'localhost';
    let dbPort = '5432';
    let dbUser = 'postgres';
    let dbPass = '';
    let rutaBase;
    let jwtKey = '';
    let passwordTemporal = '';

    if (generarBackend) {
      exigirArchivo(path.join(RUTA_PLANTILLA_BACK, 'init.sql'));
      console.log('\nConfiguración de PostgreSQL');
      esquema = validarIdentificadorPostgres(
        (await preguntar(rl, `Nombre del esquema [${identificadorBase}]: `)).trim() || identificadorBase,
        'El esquema'
      );
      nombreBd = validarIdentificadorPostgres(
        (await preguntar(rl, `Nombre de la base de datos [${identificadorBase}_db]: `)).trim() || `${identificadorBase}_db`,
        'La base de datos'
      );
      dbHost = (await preguntar(rl, 'Host de PostgreSQL [localhost]: ')).trim() || 'localhost';
      if (/[;\r\n]/.test(dbHost)) throw new Error('El host de PostgreSQL contiene caracteres no válidos.');

      const puertoIngresado = (await preguntar(rl, 'Puerto de PostgreSQL [5432]: ')).trim() || '5432';
      const puerto = Number(puertoIngresado);
      if (!Number.isInteger(puerto) || puerto < 1 || puerto > 65535) {
        throw new Error('El puerto de PostgreSQL debe ser un número entero entre 1 y 65535.');
      }
      dbPort = String(puerto);
      dbUser = (await preguntar(rl, 'Usuario de PostgreSQL [postgres]: ')).trim() || 'postgres';
      if (/[\r\n]/.test(dbUser)) throw new Error('El usuario de PostgreSQL contiene caracteres no válidos.');
      const respuestaPassword = await preguntarSecreto(
        rl,
        'Contraseña de PostgreSQL (se guardará en el .env generado): '
      );
      rl = respuestaPassword.rl;
      if (respuestaPassword.cancelado) throw new Error('Generación cancelada por el usuario.');
      dbPass = respuestaPassword.valor;
      if (/[\r\n]/.test(dbPass)) throw new Error('La contraseña de PostgreSQL no puede contener saltos de línea.');

      rutaBase = normalizarRutaBase(
        (await preguntar(rl, `PathBase del backend [/${esquema}/api]: `)).trim() || `/${esquema}/api`
      );
      jwtKey = generarClaveJwt();
      passwordTemporal = generarPasswordTemporal();
    } else {
      rutaBase = normalizarRutaBase(
        (await preguntar(rl, 'PathBase del backend existente [/PlantillaBackend/api]: ')).trim() ||
          '/PlantillaBackend/api'
      );
    }

    const rutaFront = path.join(RUTA_DESTINO_BASE, nombre);
    const rutaBack = path.join(RUTA_DESTINO_BASE, `${nombre}-api`);
    if (estaDentroDe(RUTA_PLANTILLA_FRONT, rutaFront) || estaDentroDe(RUTA_PLANTILLA_BACK, rutaFront)) {
      throw new Error('El directorio de salida no puede estar dentro de una plantilla.');
    }
    if (fs.existsSync(rutaFront)) throw new Error(`Ya existe el frontend de destino: ${rutaFront}`);
    if (generarBackend && fs.existsSync(rutaBack)) throw new Error(`Ya existe el backend de destino: ${rutaBack}`);

    console.log('\nCopiando y configurando frontend...');
    copiarCarpeta(RUTA_PLANTILLA_FRONT, rutaFront);
    actualizarPaqueteFrontend(rutaFront, nombre);
    actualizarAngularJson(rutaFront, nombre);
    reemplazarEnArchivo(path.join(rutaFront, 'README.md'), '/PlantillaBackend/api', rutaBase);
    const cambiosAlmacenamiento = reemplazarPrefijoAlmacenamiento(rutaFront, 'sgvti_', `${nombre.replace(/-/g, '_')}_`);
    const proxyNombre = actualizarProxyConfig(rutaFront, apiTarget, rutaBase);
    console.log(`Frontend preparado; ${cambiosAlmacenamiento} archivo(s) usan el prefijo localStorage del nuevo sistema.`);
    console.log(`Proxy /api configurado en ${proxyNombre} con PathBase ${rutaBase}.`);

    let carpetaProyecto = null;
    let proyectoDotnet = null;

    if (generarBackend) {
      console.log('\nCopiando y configurando backend...');
      copiarCarpeta(RUTA_PLANTILLA_BACK, rutaBack);

      const proyectos = encontrarProyectosDotnet(rutaBack);
      if (proyectos.length !== 1) {
        throw new Error(`Se esperaba exactamente un .csproj y se encontraron ${proyectos.length}.`);
      }

      carpetaProyecto = path.dirname(proyectos[0]);
      proyectoDotnet = actualizarProyectoDotnet(proyectos[0], nombre, esquema);
      reemplazarEnArchivo(path.join(rutaBack, 'README.md'), '/PlantillaBackend/api', rutaBase);
      reemplazarEnArchivo(
        path.join(carpetaProyecto, 'README.md'),
        '/PlantillaBackend/api',
        rutaBase
      );
      reemplazarEnArchivo(
        path.join(carpetaProyecto, 'TestProject.http'),
        '/PlantillaBackend/api',
        rutaBase
      );
      reemplazarEnArchivo(
        path.join(carpetaProyecto, 'README.md'),
        'plantilla_db',
        nombreBd
      );
      const reemplazos = reemplazarEnCarpeta(rutaBack, 'PlantillaBackend', esquema);
      actualizarPathBaseAppsettings(carpetaProyecto, rutaBase);
      actualizarEnv(carpetaProyecto, {
        dbHost,
        dbPort,
        nombreBd,
        dbUser,
        dbPass,
        rutaBase,
        jwtKey,
        passwordTemporal
      });

      console.log(`Backend configurado; ${reemplazos} archivo(s) actualizados para el esquema ${esquema}.`);
      console.log('Las claves y contraseñas quedan en TestProject/.env, no se muestran en la consola.');

      console.log('\nRestaurando y compilando backend...');
      ejecutarDotnet(['restore', proyectoDotnet], carpetaProyecto);
      ejecutarDotnet(['build', proyectoDotnet, '--no-restore'], carpetaProyecto);
    }

    console.log('\nInstalando y compilando frontend...');
    if (fs.existsSync(path.join(rutaFront, 'package-lock.json'))) {
      ejecutarNpm(['ci'], rutaFront);
    } else {
      ejecutarNpm(['install'], rutaFront);
    }
    ejecutarNpm(['run', 'build'], rutaFront);

    console.log('\nSistema creado correctamente.');
    console.log(`Frontend: ${rutaFront}`);
    if (generarBackend) {
      console.log(`Backend: ${rutaBack}`);
      console.log(`PathBase: ${rutaBase}`);
      console.log(`Esquema: ${esquema}`);
      console.log(`Base de datos: ${nombreBd}`);
      console.log('La contraseña temporal de restablecimiento fue generada y guardada en TestProject/.env.');
      console.log('\nInicializa la base de datos manualmente:');
      console.log(`psql -h "${dbHost}" -p ${dbPort} -U "${dbUser}" -d "${nombreBd}" -f "${path.join(rutaBack, 'init.sql')}"`);
      console.log('\nPara iniciar el backend:');
      console.log(`cd "${carpetaProyecto}"`);
      console.log(`dotnet run --project "${path.basename(proyectoDotnet)}"`);
    } else {
      console.log(`API: ${apiTarget}${rutaBase}`);
    }
    console.log('\nPara iniciar el frontend:');
    console.log(`cd "${rutaFront}"`);
    console.log('npm start');
  } finally {
    rl.close();
  }
}

if (require.main === module) {
  main().catch((error) => {
    console.error(`\nNo se pudo generar el sistema: ${error.message}`);
    process.exitCode = 1;
  });
}

module.exports = {
  actualizarContenidoEnv,
  actualizarProxyConfig,
  copiarCarpeta,
  crearCadenaConexion,
  debeIgnorar,
  normalizarApiTarget,
  normalizarNombreSistema,
  normalizarRutaBase,
  validarIdentificadorPostgres
};
