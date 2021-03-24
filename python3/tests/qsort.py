# http://rosettacode.org/wiki/Compare_sorting_algorithms%27_performance
from random import random, choice


def partition(seq, pivot, iterCounter):
    low, middle, up = [], [], []
    for x in seq:
        iterCounter[0] += 1
        if x < pivot:
            low.append(x)
        elif x == pivot:
            middle.append(x)
        else:
            up.append(x)
    return low, middle, up


def qsortranpart(seq, iterCounter):
    iterCounter[0] += 1
    size = len(seq)
    if size < 2: return seq
    low, middle, up = partition(seq, choice(seq), iterCounter)
    return qsortranpart(low, iterCounter) + middle + qsortranpart(up, iterCounter)