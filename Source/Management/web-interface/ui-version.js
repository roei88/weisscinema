const fs = require('fs');
const path = require('path');
const { exec } = require('child_process');
const rimraf = require('rimraf');

const VERSION_FILE_PATH = path.resolve(__dirname, 'src', 'version.js');

exec('git describe --tags --always --dirty --first-parent', (err, stdout) => {
    if (err) throw err;
    console.log("git version:", stdout)

    const version = stdout.trim();
    const versionFileContent = `module.exports = { version: "${version}" };\n`;

    rimraf.sync(VERSION_FILE_PATH)
    fs.writeFileSync(VERSION_FILE_PATH, versionFileContent);
});

exec('git status', (err, stdout) => {
    if (err) throw err;
    console.log("git status:", stdout)
});

exec('git diff', {maxBuffer: 1024*1024*1024 }, (err, stdout) => {
    if (err) throw err;
    console.log("git diff:", stdout)
});
