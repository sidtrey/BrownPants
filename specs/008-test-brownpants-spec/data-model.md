# Data Model: Spec Kit Installation Validation

## Isolated Spec Kit Project

Temporary project created solely to exercise extension installation.

- **Root**: unique child of the operating system temporary directory.
- **Registration**: generated `.specify/extensions.yml` containing installed extension and lifecycle hooks.
- **InstalledExtension**: generated `.specify/extensions/brownpants/extension.yml` plus its command file.
- **Cleanup**: remove the exact unique root after leaving it; never touch repository `.specify` files.

## Hook Registration

Each of the ten lifecycle events must have one enabled registration, must be mandatory (`optional: false`), and must target `speckit.brownpants.context`.
