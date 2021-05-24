class FruitPrinter:
    def __init__(self, val):
        self.val = val

    def to_string(self):
        fruit = self.val['fruit']

        if (fruit == 0):
            name = "Orange"
        elif (fruit == 1):
            name = "Apple"
        elif (fruit == 2):
            name = "Banana"
        else:
            name = "unknown"
        return "Our fruit is " + name


def lookup_type(val):
    if str(val.type) == 'Fruit':
        return FruitPrinter(val)
    return None


gdb.pretty_printers.append(lookup_type)