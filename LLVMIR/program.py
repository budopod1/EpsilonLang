from llvmlite import ir
from common import *


class Program:
    def __init__(self, module):
        self.module = module
        self.functions = {}
        self.structs = {}
        self.array_ids = {}
        self.arrays = {}
        self.stdlib = {}
        self.stdlib_funcs = {}

    def add_stdlib_func(self, name, data):
        self.stdlib[name] = data
        self.stdlib_funcs[name] = ir.Function(
            self.module, make_function_type_(
                self, data["return_type_"], data["arguments"]
            ), name=name
        )

    def call_stdlib(self, builder, name, params, param_types_, result_type_):
        func = self.stdlib[name]
        converted_params = [
            convert_type_(self, builder, param, param_type_, argument)
            for param, param_type_, argument in zip(params, param_types_, func["arguments"])
        ]
        return convert_type_(
            self, builder, builder.call(self.stdlib_funcs[name], converted_params),
            func["return_type_"], result_type_
        )

    def add_function(self, function):
        self.functions[function.id_] = function

    def add_struct(self, struct):
        self.structs[struct.name] = struct

    def add_array(self, array):
        self.arrays[array.id_] = array
