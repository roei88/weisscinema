const fs = require('fs');
const path = require('path');
const { protoc } = require('protoc');

const COMPILED_PROTO_FILES_DIRECTORY_PATH = path.resolve(__dirname, '..', 'src', 'proto');
const PROTO_FILES_DIRECTORY_PATH = path.resolve(__dirname, '..', '..', '..', 'ServicesDefinitions');

const protoFileNames = [
    'Messages.proto',
    'Logs.proto',
    'VPU.proto',
    'CommonMessages.proto',
    'PipObjectsProperties.proto'
];

const compiledProtoDirectoryFileNames = fs.readdirSync(COMPILED_PROTO_FILES_DIRECTORY_PATH);

for (const fileName of compiledProtoDirectoryFileNames) {
    const fileExtension = fileName.substr(fileName.lastIndexOf('.') + 1);

    if (fileExtension === 'js') {
        const fileAbsolutePath = path.resolve(COMPILED_PROTO_FILES_DIRECTORY_PATH, fileName);

        fs.unlinkSync(fileAbsolutePath);
    }
}

const protoFilesPaths = protoFileNames.map(fileName => path.resolve(PROTO_FILES_DIRECTORY_PATH, fileName));

protoc([
    `--proto_path=${PROTO_FILES_DIRECTORY_PATH}`,
    `--js_out=import_style=commonjs,binary:${COMPILED_PROTO_FILES_DIRECTORY_PATH}`,
    ...protoFilesPaths
], {}, err => {
    if (err) throw err;
});
