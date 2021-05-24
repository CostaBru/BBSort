#inject this code to C:\msys64\mingw64\share\gcc-10.2.0\python\libstdcxx\v6\printers.py

class KbPoolVectorPrinter:
    "Print a pool::vector"

    class _iterator(Iterator):
        def __init__ (self, start, size):
            self.item = start
            self.size = size
            self.count = 0

        def __iter__(self):
            return self

        def __next__(self):
            count = self.count
            self.count = self.count + 1
            if self.count == self.size + 1:
                raise StopIteration
            elt = self.item.dereference()
            self.item = self.item + 1
            return ('[%d]' % count, elt)

    def __init__(self, typename, val):
        self.typename = strip_versioned_namespace(typename)
        self.val = val

    def children(self):
        return self._iterator(self.val['array'], self.val['length'])

    def to_string(self):
        start = self.val['array']
        size = self.val['length']

        return ('%s of length %d, capacity %d'
                % (self.typename, int(self.val['length']), int(self.val['capacity'])))

    def display_hint(self):
        return self.to_string()

#register new printer in def build_libstdcxx_dictionary()

libstdcxx_printer.add_container('pool::', 'vector', KbPoolVectorPrinter)
libstdcxx_printer.add('pool::__debug::vector', KbPoolVectorPrinter)

libstdcxx_printer.add_container('pool::', 'vector_lazy', KbPoolVectorPrinter)
libstdcxx_printer.add('pool::__debug::vector_lazy', KbPoolVectorPrinter)

