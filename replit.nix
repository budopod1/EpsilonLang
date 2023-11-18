{ pkgs }: {
    env = {
        LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath [
            pkgs.stdenv.cc.cc.lib
        ];
    };
    deps = [
        pkgs.python39Packages.python-lsp-server
        pkgs.python310Packages.virtualenv
        pkgs.ack
        pkgs.python310
        pkgs.mono5
        pkgs.coreutils
        pkgs.git
        pkgs.llvmPackages_14.libllvm
        pkgs.omnisharp-roslyn
    ];
}
