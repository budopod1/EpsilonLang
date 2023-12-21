from llvmlite import ir
from common import *


def length(program, builder, params, param_types_):
    param, = params
    result_ptr = builder.gep(param, [i64_of(0), i32_of(2)])
    return builder.load(result_ptr), W64


def capacity(program, builder, params, param_types_):
    param, = params
    result_ptr = builder.gep(param, [i64_of(0), i32_of(1)])
    return builder.load(result_ptr), W64


def append(program, builder, params, param_types_):
    array, value = params
    array_type_, value_type_ = param_types_
    length_ptr = builder.gep(array, [i64_of(0), i32_of(2)])
    length = builder.load(length_ptr)
    elem_type_ = array_type_["generics"][0]
    elem_size = program.sizeof(builder, make_type_(program, array_generic))
    program.call_extern(
        builder, "incrementLength", [array, elem_size], [
            ArrayW8,
            W8
        ], VOID
    )
    content_ptr = builder.gep(param, [i64_of(0), i32_of(3)])
    content = builder.bitcast(
        builder.load(content_ptr), 
        make_type_(program, elem_type_).as_pointer()
    )
    end_ptr = builder.gep(content, [length])
    value_casted = convert_type_(program, builder, value, value_type_, elem_type_)
    builder.store(value_casted, end_ptr)
    return None, VOID


def require_capacity(program, builder, params, param_types_):
    array, capacity = params
    array_type_, _ = param_types_
    elem_type_ = array_type_["generics"][0]
    elem_size = program.sizeof(builder, make_type_(program, elem_type_))
    program.call_extern(
        builder, "requireCapacity", [], [array, capacity, elem_size], [
            ArrayW8,
            W64,
            W64
        ], VOID
    )
    return None, VOID


def shrink_mem(program, builder, params, param_types_):
    array, = params
    array_type_, = param_types_
    elem_type_ = array_type_["generics"][0]
    elem_size = program.sizeof(builder, make_type_(program, elem_type_))
    program.call_extern(
        builder, "shrinkMem", [], [array, elem_size], [
            ArrayW8, W64
        ], VOID
    )
    return None, VOID


def pop(program, builder, params, param_types_):
    array, idx = params
    array_type_, _ = param_types_
    elem_type_ = array_type_["generics"][0]
    elem_ir_type = make_type_(program, elem_type_)
    content_ptr = builder.load(builder.gep(array, [i64_of(0), i32_of(3)]))
    content_ptr_casted = builder.bicast(content_ptr, elem_ir_type.as_pointer())
    elem = builder.load(builder.gep(content_ptr_casted, [idx]))
    elem_size = program.sizeof(builder, elem_ir_type)
    program.call_extern(
        builder, "removeAt", [], [array, idx, elem_size], [
            ArrayW8, W64, 
            W64
        ], VOID
    )
    program.decr_ref(builder, elem, elem_type_)
    return elem, elem_type_


def insert(program, builder, params, param_types_):
    array, idx, value = params
    array_type_, _, value_type_ = param_types_
    elem_type_ = array_type_["generics"][0]
    casted_value = convert_type(program, builder, value, value_type_, elem_type_)
    elem_ir_type = make_type_(program, elem_type_)
    elem_size = program.sizeof(builder, elem_ir_type)
    program.call_extern(
        builder, "insertSpace", [], [array, idx, elem_size], [
            ArrayW8, W64, 
            W64
        ], VOID
    )
    content_ptr = builder.load(builder.gep(array, [i64_of(0), i32_of(3)]))
    content_ptr_casted = builder.bicast(content_ptr, elem_ir_type.as_pointer())
    builder.store(casted_value, builder.gep(content_ptr_casted, [idx]))
    return None, VOID


def clone(program, builder, params, param_types_):
    array, = params
    array_type_, = param_types_
    elem_type_ = array_type_["generics"][0]
    elem = progra.make_elem(builder, elem_type_)
    new_array = program.call_extern(
        builder, "clone", [array, elem], [
            ArrayW8, W64
        ], ArrayW8
    )
    return array, ArrayW8


def extend(program, builder, params, param_types_):
    array1, array2 = params
    array_type_, _ = param_types_ # array types_ will be the same
    elem_type_ = array_type_["generics"][0]
    elem = program.make_elem(builder, elem_type_)
    program.call_extern(
        builder, "extend", [array1, array2, elem], [
            ArrayW8, ArrayW8, W64
        ], VOID
    )
    return None, VOID


def concat(program, builder, params, param_types_):
    array1, array2 = params
    array_type_, _ = param_types_ # array types_ will be the same
    elem_type_ = array_type_["generics"][0]
    elem = program.make_elem(builder, elem_type_)
    return program.call_extern(
        builder, "concat", [array1, array2, elem], [
            ArrayW8, ArrayW8, W64
        ], array_type_
    ), array_type_


def make_range_array_1(program, builder, params, param_types_):
    end, = params
    return program.call_extern(
        builder, "rangeArray1", [end], [
            Z32
        ], ArrayW8
    ), ArrayW8


def make_range_array_2(program, builder, params, param_types_):
    start, end, = params
    return program.call_extern(
        builder, "rangeArray2", [start, end], [
            Z32, Z32
        ], ArrayW8
    ), ArrayW8


def make_range_array_3(program, builder, params, param_types_):
    start, end, step = params
    return program.call_extern(
        builder, "rangeArray3", [start, end, step], [
            Z32, Z32,
            Z32
        ], ArrayW8
    ), ArrayW8


def abs_(program, builder, params, param_types_):
    value, = params
    return program.call_extern(
        builder, "abs", [value, i1_of(0)], [Z32, Bool], W32
    ), W32


def fabs(program, builder, params, param_types_):
    value, = params
    return program.call_extern(
        builder, "fabs", [value], [Q64],
        Q64
    ), Q64


def stringify(program, builder, params, param_types_):
    param, = params
    param_type_, = param_types_
    return program.stringify(builder, param, param_type_), String


def print_(program, builder, params, param_types_):
    param, = params
    param_type_, = param_types_
    string = program.stringify(builder, param, param_type_)
    program.call_extern(builder, "print", [string], [String], VOID)
    return None, VOID


def println(program, builder, params, param_types_):
    param, = params
    param_type_, = param_types_
    string = program.stringify(builder, param, param_type_)
    program.call_extern(builder, "println", [string], [String], VOID)
    return None, VOID


def left_pad(program, builder, params, param_types_):
    arr, len, chr = params
    program.call_extern(builder, "leftPad", [arr, len, chr], [String, W64, Byte], VOID)
    return None, VOID


def right_pad(program, builder, params, param_types_):
    arr, len, chr = params
    program.call_extern(builder, "rightPad", [arr, len, chr], [String, W64, Byte], VOID)
    return None, VOID


def slice_(program, builder, params, param_types_):
    arr, start, end = params
    arr_type_, _, _ = param_types_
    generic_type_ = arr_type_["generics"][0]
    elem = program.make_elem(builder, generic_type_)
    return program.call_extern(
        builder, "slice", [arr, start, end, elem], 
        [ArrayW8, W64, W64, W64], ArrayW8
    ), ArrayW8


def countChr(program, builder, params, param_types_):
    str, chr = params
    return program.call_extern(
        builder, "countChr", [str, chr],
        [String, Byte], W64
    ), W64


def count(program, builder, params, param_types_):
    arr, sub = params
    arr_type_, _ = param_types_
    generic_type_ = arr_type_["generics"][0]
    elem_size = program.sizeof(builder, make_type_(program, generic_type_))
    return program.call_extern(
        builder, "count", [arr, sub, elem_size], 
        [ArrayW8, ArrayW8, W64], ArrayW8
    ), ArrayW8


def overlapCount(program, builder, params, param_types_):
    arr, sub = params
    arr_type_, _ = param_types_
    generic_type_ = arr_type_["generics"][0]
    elem_size = program.sizeof(builder, make_type_(program, generic_type_))
    return program.call_extern(
        builder, "overlapCount", [arr, sub, elem_size], 
        [ArrayW8, ArrayW8, W64], ArrayW8
    ), ArrayW8


def nest(program, builder, params, param_types_):
    arr, = params
    arr_type_, = param_types_
    generic_type_ = arr_type_["generics"][0]
    elem = program.make_elem(builder, generic_type_)
    return program.call_extern(
        builder, "nest", [arr, elem], [ArrayW8, W64], ArrayW8
    ), ArrayW8


def split(program, builder, params, param_types_):
    arr, sub = params
    arr_type_, _ = param_types_
    generic_type_ = arr_type_["generics"][0]
    elem = program.make_elem(builder, generic_type_)
    return program.call_extern(
        builder, "split", [arr, sub, elem], [ArrayW8, ArrayW8, W64], ArrayW8
    ), ArrayW8


def starts_with(program, builder, params, param_types_):
    arr, sub = params
    arr_type_, _ = param_types_
    generic_type_ = arr_type_["generics"][0]
    elem_size = program.sizeof(builder, make_type_(program, generic_type_))
    return program.call_extern(
        builder, "startsWith", [arr, sub, elem_size], 
        [ArrayW8, ArrayW8, W64], Bool
    ), Bool


def ends_with(program, builder, params, param_types_):
    arr, sub = params
    arr_type_, _ = param_types_
    generic_type_ = arr_type_["generics"][0]
    elem_size = program.sizeof(builder, make_type_(program, generic_type_))
    return program.call_extern(
        builder, "endsWith", [arr, sub, elem_size], 
        [ArrayW8, ArrayW8, W64], Bool
    ), Bool


def equals(program, builder, params, param_types_):
    v1, v2 = params
    type_1, type_2 = param_types_
    assert type_1 == type_2
    return program.value_equals_depth_1(builder, type_1, v1, v2, False), Bool


def not_equals(program, builder, params, param_types_):
    v1, v2 = params
    type_1, type_2 = param_types_
    assert type_1 == type_2
    return program.value_equals_depth_1(builder, type_1, v1, v2, True), Bool


def equals_depth(program, builder, params, param_types_):
    v1, v2, depth_ir_const = params
    type_1, type_2, _ = param_types_
    assert type_1 == type_2
    depth = depth_ir_const.json_const["value"]
    return program.value_equals(builder, type_1, v1, v2, depth, False), Bool


def not_equals_depth(program, builder, params, param_types_):
    v1, v2, depth_ir_const = params
    type_1, type_2, _ = param_types_
    assert type_1 == type_2
    depth = depth_ir_const.json_const["value"]
    return program.value_equals(builder, type_1, v1, v2, depth, True), Bool


BUILTINS = {
    -1: {
        "func": length,
        "params": [ArrayW8]
    },
    -2: {
        "func": capacity,
        "params": [ArrayW8]
    },
    -3: {
        "func": append,
        "params": [ArrayW8, None]
    },
    -4: {
        "func": require_capacity,
        "params": [ArrayW8, W64]
    },
    -5: {
        "func": shrink_mem,
        "params": [
            ArrayW8
        ]
    },
    -6: {
        "func": pop,
        "params": [
            ArrayW8, W64
        ]
    },
    -7: {
        "func": insert,
        "params": [
            ArrayW8, W64,
            None
        ]
    },
    -8: {
        "func": clone,
        "params": [
            ArrayW8
        ]
    },
    -9: {
        "func": extend,
        "params": [
            ArrayW8, ArrayW8
        ]
    },
    -10: {
        "func": concat,
        "params": [
            ArrayW8, ArrayW8
        ]
    },
    -11: {
        "func": make_range_array_1,
        "params": [
            Z32
        ]
    },
    -12: {
        "func": make_range_array_2,
        "params": [
            Z32, Z32
        ]
    },
    -13: {
        "func": make_range_array_3,
        "params": [
            Z32, Z32,
            Z32
        ]
    },
    -14: {
        "func": abs_,
        "params": [Z32]
    },
    -15: {
        "func": fabs,
        "params": [Q64]
    },
    -16: {
        "func": concat,
        "params": [
            ArrayW8, ArrayW8
        ]
    },
    -17: {
        "func": stringify,
        "params": [None]
    },
    -18: {
        "func": print_,
        "params": [None]
    },
    -19: {
        "func": println,
        "params": [None]
    },
    -20: {
        "func": left_pad,
        "params": [String, W64, Byte]
    },
    -21: {
        "func": right_pad,
        "params": [String, W64, Byte]
    },
    -22: {
        "func": slice_,
        "params": [ArrayW8, W64, W64]
    },
    -23: {
        "func": countChr,
        "params": [String, Byte]
    },
    -24: {
        "func": count,
        "params": [ArrayW8, ArrayW8]
    },
    -25: {
        "func": overlapCount,
        "params": [ArrayW8, ArrayW8]
    },
    -26: {
        "func": nest,
        "params": [ArrayW8]
    },
    -27: {
        "func": split,
        "params": [ArrayW8, ArrayW8]
    },
    -28: {
        "func": starts_with,
        "params": [ArrayW8, ArrayW8]
    },
    -29: {
        "func": ends_with,
        "params": [ArrayW8, ArrayW8]
    },
    -30: {
        "func": equals,
        "params": [None, None]
    },
    -31: {
        "func": not_equals,
        "params": [None, None]
    },
    -32: {
        "func": equals_depth,
        "params": [None, None, None]
    },
    -33: {
        "func": not_equals_depth,
        "params": [None, None, None]
    }
}
