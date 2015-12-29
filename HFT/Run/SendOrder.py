import sys

'''
print (sys.path)
sys.path.append("E:\\Python\\Python35\\Lib\\")
sys.path.append("E:\\Python\\Python35\\Lib\\site-packages\\")
print (sys.path)
'''
import re

import json

import requests

rc=re.compile("(\w+)|'(.%?)'")

cookie=sys.argv[1]

ck=dict(JSESSIONID=cookie)

print(ck)

SendOrder=sys.argv[2]

print(SendOrder)

r=requests.get(SendOrder,ck);

