import unittest
from cloud import normalize, number

class CloudTests(unittest.TestCase):
    def sample(self):
        return {'code':0,'data':{'outdoor':{'temperature':{'value':'50','unit':'°F','time':'1000'},'feels_like':{'value':'46.4','unit':'°F','time':'1000'},'humidity':{'value':'59','unit':'%','time':'1000'}},'wind':{'wind_speed':{'value':'1','unit':'m/s','time':'1000'}}}}
    def test_conversion(self):
        v=normalize(self.sample(),1000)
        self.assertEqual(v['temp'],10)
        self.assertAlmostEqual(v['feelslike'],8)
        self.assertEqual(v['wind'],3.6)
        self.assertEqual(v['ts'],1000000)
    def test_missing_is_null(self):
        v=normalize(self.sample(),1000)
        self.assertIsNone(v['pressure']);self.assertIsNone(v['rain'])
    def test_no_fake_freshness(self):
        self.assertEqual(normalize(self.sample(),2000)['ts'],1000000)
    def test_reject_error_and_future(self):
        with self.assertRaises(ValueError):normalize({'code':-1},1000)
        with self.assertRaises(ValueError):normalize(self.sample(),100)
    def test_unknown_unit_and_nonfinite(self):
        self.assertIsNone(number({'value':'12','unit':'unknown'},'temp'))
        self.assertIsNone(number({'value':'NaN','unit':'°C'},'temp'))
    def test_secondary_timestamp(self):
        p=self.sample();p['data']['outdoor']['feels_like']['time']='500'
        self.assertIsNone(normalize(p,1000)['feelslike'])

if __name__=='__main__':unittest.main()
